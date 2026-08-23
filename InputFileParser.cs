using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LPR381Solver.Core;

namespace LPR381Solver.IO
{
    /// <summary>
    /// Parses the input file format specified in the brief into an LPModel:
    ///
    ///   Line 1            : max/min, then one signed coefficient per decision variable
    ///   Lines 2..(n-1)     : one constraint per line - signed technological
    ///                        coefficients, a relation (&lt;=, &gt;=, =), then an RHS
    ///   Last line          : one sign restriction per decision variable (+, -, urs, int, bin)
    ///
    /// Deliberately tokenizes rather than splitting on whitespace, because the
    /// brief's own example glues the relation to the RHS with no space
    /// ("&lt;=40"), and a real submitted file might do either.
    /// </summary>
    public static class InputFileParser
    {
        // Matches a relation operator OR a (optionally signed) number, in the
        // order they appear - so "<=40" and "<= 40" tokenize identically.
        private static readonly Regex ConstraintTokenPattern =
            new(@"<=|>=|=|[+-]?\d+(?:\.\d+)?", RegexOptions.Compiled);

        public static LPModel ParseFile(string path)
        {
            if (!File.Exists(path))
                throw new InputFileParseException($"Input file not found: \"{path}\".");

            var rawLines = File.ReadAllLines(path);
            return Parse(rawLines);
        }

        public static LPModel Parse(IEnumerable<string> rawLines)
        {
            // Blank lines are tolerated (e.g. trailing newline, or a spacer
            // someone added by hand) but otherwise ignored - they don't count
            // toward line numbers used in error messages, which instead refer
            // to *content* line numbers (1 = objective, last = sign restrictions).
            var lines = rawLines.Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

            if (lines.Count < 3)
                throw new InputFileParseException(
                    "Input file must contain at least an objective line, one constraint line, and a sign restriction line.");

            var (objectiveType, objectiveCoefficients) = ParseObjectiveLine(lines[0]);
            int n = objectiveCoefficients.Length;

            var constraintLines = lines.Skip(1).Take(lines.Count - 2).ToList();
            if (constraintLines.Count == 0)
                throw new InputFileParseException("Input file must contain at least one constraint.");

            var constraints = new List<Constraint>();
            for (int i = 0; i < constraintLines.Count; i++)
                constraints.Add(ParseConstraintLine(constraintLines[i], contentLineNumber: i + 2, expectedVariableCount: n));

            var signRestrictions = ParseSignRestrictionLine(lines[^1], n);

            // LPModel's own constructor calls Validate() again, but doing it
            // here too lets us report a parser-flavoured error message rather
            // than a generic one if something still doesn't line up.
            var model = new LPModel(objectiveType, objectiveCoefficients, constraints, signRestrictions);
            return model;
        }

        private static (ObjectiveType, double[]) ParseObjectiveLine(string line)
        {
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
                throw new InputFileParseException(
                    $"Line 1: expected \"max\"/\"min\" followed by at least one signed coefficient. Got: \"{line}\".");

            ObjectiveType type = tokens[0].ToLowerInvariant() switch
            {
                "max" => ObjectiveType.Max,
                "min" => ObjectiveType.Min,
                _ => throw new InputFileParseException($"Line 1: expected \"max\" or \"min\", got \"{tokens[0]}\".")
            };

            var coefficients = new double[tokens.Length - 1];
            for (int i = 1; i < tokens.Length; i++)
            {
                if (!TryParseSignedNumber(tokens[i], out var value))
                    throw new InputFileParseException(
                        $"Line 1: could not parse objective coefficient \"{tokens[i]}\" - expected a signed number, e.g. +2 or -3.");
                coefficients[i - 1] = value;
            }

            return (type, coefficients);
        }

        private static Constraint ParseConstraintLine(string line, int contentLineNumber, int expectedVariableCount)
        {
            var tokens = ConstraintTokenPattern.Matches(line).Select(m => m.Value).ToList();

            int relationIndex = tokens.FindIndex(t => t is "<=" or ">=" or "=");
            if (relationIndex < 0)
                throw new InputFileParseException(
                    $"Line {contentLineNumber}: no relation (<=, >=, or =) found in constraint \"{line}\".");

            var coefficientTokens = tokens.Take(relationIndex).ToList();
            if (coefficientTokens.Count != expectedVariableCount)
                throw new InputFileParseException(
                    $"Line {contentLineNumber}: expected {expectedVariableCount} technological coefficients (one per decision variable), found {coefficientTokens.Count}.");

            var coefficients = new double[expectedVariableCount];
            for (int i = 0; i < coefficientTokens.Count; i++)
            {
                if (!TryParseSignedNumber(coefficientTokens[i], out var value))
                    throw new InputFileParseException(
                        $"Line {contentLineNumber}: could not parse technological coefficient \"{coefficientTokens[i]}\" - expected a signed number, e.g. +11 or -8.");
                coefficients[i] = value;
            }

            var relation = tokens[relationIndex] switch
            {
                "<=" => Relation.LessOrEqual,
                ">=" => Relation.GreaterOrEqual,
                "=" => Relation.Equal,
                _ => throw new InvalidOperationException("Unreachable - relationIndex only matches these three tokens.")
            };

            var rhsTokens = tokens.Skip(relationIndex + 1).ToList();
            if (rhsTokens.Count != 1)
                throw new InputFileParseException(
                    $"Line {contentLineNumber}: expected exactly one right-hand-side value after the relation, found {rhsTokens.Count}.");

            if (!double.TryParse(rhsTokens[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var rhs))
                throw new InputFileParseException(
                    $"Line {contentLineNumber}: could not parse right-hand-side value \"{rhsTokens[0]}\".");

            return new Constraint(coefficients, relation, rhs);
        }

        private static SignRestriction[] ParseSignRestrictionLine(string line, int expectedVariableCount)
        {
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != expectedVariableCount)
                throw new InputFileParseException(
                    $"Sign restriction line: expected {expectedVariableCount} entries (one per decision variable), found {tokens.Length}.");

            var result = new SignRestriction[expectedVariableCount];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = tokens[i].ToLowerInvariant() switch
                {
                    "+" => SignRestriction.Positive,
                    "-" => SignRestriction.Negative,
                    "urs" => SignRestriction.Unrestricted,
                    "int" => SignRestriction.Integer,
                    "bin" => SignRestriction.Binary,
                    _ => throw new InputFileParseException(
                        $"Sign restriction line: unrecognized token \"{tokens[i]}\" - expected +, -, urs, int, or bin.")
                };
            }

            return result;
        }

        /// <summary>
        /// The brief requires every objective/technological coefficient to
        /// carry an explicit sign, so a bare "40" here is rejected (that's
        /// only valid for a constraint's RHS, which is parsed separately).
        /// </summary>
        private static bool TryParseSignedNumber(string token, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(token)) return false;
            if (token[0] != '+' && token[0] != '-') return false;
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
