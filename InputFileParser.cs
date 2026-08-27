using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LPR381Solver.Core;

namespace LPR381Solver.IO
{
    public class InputFileParser
    {
        public static LPModel Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new InputFileParseException($"File not found: {filePath}");

            var lines = File.ReadAllLines(filePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count < 3) throw new InputFileParseException("Invalid file format.");

            // Placeholder basic parser logic to satisfy the architecture requirement
            // Requires objective line, constraint lines, and sign restriction lines.
            ObjectiveType type = lines[0].StartsWith("max", StringComparison.OrdinalIgnoreCase) ? ObjectiveType.Max : ObjectiveType.Min;
            
            var objCoeffs = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Skip(1).Select(double.Parse).ToArray();

            var constraints = new List<Constraint>();
            for (int i = 1; i < lines.Count - 1; i++)
            {
                var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var relation = parts[parts.Length - 2] switch
                {
                    "<=" => Relation.LessOrEqual,
                    ">=" => Relation.GreaterOrEqual,
                    "=" => Relation.Equal,
                    _ => throw new InputFileParseException("Invalid relation.")
                };
                double rhs = double.Parse(parts.Last());
                var coeffs = parts.Take(parts.Length - 2).Select(double.Parse).ToArray();
                constraints.Add(new Constraint(coeffs, relation, rhs));
            }

            var signParts = lines.Last().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var signs = signParts.Select(s => s.ToLower() switch {
                "int" => SignRestriction.Integer,
                "bin" => SignRestriction.Binary,
                "+" => SignRestriction.Positive,
                "-" => SignRestriction.Negative,
                _ => SignRestriction.Unrestricted
            }).ToArray();

            return new LPModel(type, objCoeffs, constraints, signs);
        }
    }

    public class InputFileParseException : Exception
    {
        public InputFileParseException(string message) : base(message) { }
    }
}