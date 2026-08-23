using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381Solver.Core
{
    /// <summary>
    /// The full mathematical model exactly as specified in the input file - the
    /// single source of truth that the parser writes to and every algorithm,
    /// the output writer, and every sensitivity operation reads from.
    ///
    /// Deliberately format-agnostic: this stores the LP/IP model *as entered*
    /// (per the brief: "Not the canonical forms of the different algorithms...
    /// or the Relaxed Linear Programming Model"). Converting this into a
    /// specific algorithm's working form is CanonicalFormBuilder's job, not this
    /// class's - keeps the "what the model says" and "how we solve it" concerns
    /// separate, which matters once five different algorithms all need a
    /// tableau built from the same model.
    /// </summary>
    public class LPModel
    {
        public ObjectiveType ObjectiveType { get; set; }
        public double[] ObjectiveCoefficients { get; }
        public List<Constraint> Constraints { get; }
        public SignRestriction[] SignRestrictions { get; }

        public int VariableCount => ObjectiveCoefficients.Length;
        public int ConstraintCount => Constraints.Count;

        public LPModel(
            ObjectiveType objectiveType,
            double[] objectiveCoefficients,
            List<Constraint> constraints,
            SignRestriction[] signRestrictions)
        {
            ObjectiveType = objectiveType;
            ObjectiveCoefficients = objectiveCoefficients ?? throw new ArgumentNullException(nameof(objectiveCoefficients));
            Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            SignRestrictions = signRestrictions ?? throw new ArgumentNullException(nameof(signRestrictions));

            Validate();
        }

        /// <summary>
        /// Checks internal consistency (matching variable counts, at least one
        /// variable/constraint). Called on construction and re-callable by the
        /// parser so a malformed input file fails with a clear message instead
        /// of crashing deep inside an algorithm - this is what "Error Handling"
        /// (5 marks) hooks into for input-level problems.
        /// </summary>
        public void Validate()
        {
            if (VariableCount == 0)
                throw new ModelValidationException("Model must have at least one decision variable.");

            if (Constraints.Count == 0)
                throw new ModelValidationException("Model must have at least one constraint.");

            if (SignRestrictions.Length != VariableCount)
                throw new ModelValidationException(
                    $"Expected {VariableCount} sign restrictions (one per decision variable), got {SignRestrictions.Length}.");

            for (int i = 0; i < Constraints.Count; i++)
            {
                if (Constraints[i].Coefficients.Length != VariableCount)
                    throw new ModelValidationException(
                        $"Constraint {i + 1} has {Constraints[i].Coefficients.Length} coefficients, expected {VariableCount}.");
            }
        }

        public bool IsBinaryModel => SignRestrictions.All(s => s == SignRestriction.Binary);
        public bool IsIntegerModel => SignRestrictions.Any(s => s == SignRestriction.Integer || s == SignRestriction.Binary);
        public bool IsPureLp => !IsIntegerModel;

        /// <summary>
        /// Deep copy. Branch &amp; Bound needs a fresh model per sub-problem (e.g.
        /// x_k = 0 fixed, x_k = 1 fixed) and sensitivity analysis needs to try
        /// changes without disturbing the model that produced the optimal
        /// tableau everything else is displayed against.
        /// </summary>
        public LPModel Clone() => new LPModel(
            ObjectiveType,
            (double[])ObjectiveCoefficients.Clone(),
            Constraints.Select(c => c.Clone()).ToList(),
            (SignRestriction[])SignRestrictions.Clone());

        /// <summary>
        /// Renders the model back into the input file's own notation. Reused by
        /// the output writer for the "model as entered" section and handy for
        /// debugging the parser (round-trip: parse a file, print it, diff it).
        /// </summary>
        public override string ToString()
        {
            var lines = new List<string>();
            var obj = ObjectiveType == ObjectiveType.Max ? "max" : "min";
            var coeffs = string.Join(" ", ObjectiveCoefficients.Select(c => (c >= 0 ? "+" : "") + c.ToString("0.###")));
            lines.Add($"{obj} {coeffs}");
            lines.AddRange(Constraints.Select(c => c.ToString()));
            lines.Add(string.Join(" ", SignRestrictions.Select(FormatSignRestriction)));
            return string.Join(Environment.NewLine, lines);
        }

        private static string FormatSignRestriction(SignRestriction s) => s switch
        {
            SignRestriction.Positive => "+",
            SignRestriction.Negative => "-",
            SignRestriction.Unrestricted => "urs",
            SignRestriction.Integer => "int",
            SignRestriction.Binary => "bin",
            _ => "?"
        };
    }

    /// <summary>
    /// Thrown when an input file parses syntactically but describes an
    /// inconsistent model (mismatched counts, empty model, etc).
    /// </summary>
    public class ModelValidationException : Exception
    {
        public ModelValidationException(string message) : base(message) { }
    }
}
