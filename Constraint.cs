using System;
using System.Linq;

namespace LPR381Solver.Core
{
    /// <summary>
    /// A single constraint exactly as read from the input file: signed
    /// technological coefficients, a relation (&lt;=, &gt;=, =), and a right-hand side.
    /// </summary>
    public class Constraint
    {
        public double[] Coefficients { get; }
        public Relation Relation { get; set; }
        public double Rhs { get; set; }

        public Constraint(double[] coefficients, Relation relation, double rhs)
        {
            Coefficients = coefficients ?? throw new ArgumentNullException(nameof(coefficients));
            Relation = relation;
            Rhs = rhs;
        }

        /// <summary>
        /// Deep copy. Sensitivity analysis (e.g. "apply and display a change of a
        /// selected RHS value") must be able to try a modification without
        /// mutating the model that produced the original optimal tableau.
        /// </summary>
        public Constraint Clone() => new Constraint((double[])Coefficients.Clone(), Relation, Rhs);

        public override string ToString()
        {
            var relSymbol = Relation switch
            {
                Relation.LessOrEqual => "<=",
                Relation.GreaterOrEqual => ">=",
                Relation.Equal => "=",
                _ => "?"
            };
            var coeffs = string.Join(" ", Coefficients.Select(c => (c >= 0 ? "+" : "") + c.ToString("0.###")));
            return $"{coeffs} {relSymbol} {Rhs.ToString("0.###")}";
        }
    }
}
