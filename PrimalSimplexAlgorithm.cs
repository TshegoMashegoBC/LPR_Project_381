using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// A working Primal Simplex solver using the Big-M method, built as
    /// scaffolding so the menu/output pipeline has something real to run
    /// end-to-end before Person B's actual Primal Simplex / Revised Simplex
    /// work is done.
    ///
    /// This is NOT a claim on Person B's marks - it's here so:
    ///   (a) the rest of the team can see the full pipeline work today, and
    ///   (b) Person B has a working reference to check their own
    ///       implementation against (or extend/replace outright).
    ///
    /// Known limitations (by design, not oversight):
    ///   - Solves the continuous LP relaxation only. It does NOT enforce
    ///     integer/binary restrictions - that's Branch &amp; Bound's job
    ///     (Person C / Person D), which will call this same tableau pipeline
    ///     per sub-problem with tightened bounds.
    ///   - Uses Bland's rule (smallest-index entering/leaving variable,
    ///     rather than "most negative"/"smallest ratio" with arbitrary
    ///     tie-breaking) specifically because it provably prevents cycling -
    ///     important for a reference implementation meant to always terminate.
    /// </summary>
    public class PrimalSimplexAlgorithm : IAlgorithm
    {
        public string Name => "Primal Simplex (Big-M) [reference - replace with Person B's implementation]";

        private const double BigM = 1_000_000;
        private const double Tolerance = 1e-9;
        private const int MaxIterations = 500;

        public SolveResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.BuildInitialTableau(model);
            ApplyBigMPenalty(tableau);

            var iterations = new List<Tableau> { tableau.Clone() };
            int iterationCount = 0;

            while (!tableau.IsOptimalForMax(Tolerance))
            {
                if (++iterationCount > MaxIterations)
                    throw new InvalidOperationException(
                        $"{Name}: exceeded {MaxIterations} iterations without reaching optimality - possible cycling or a modelling error.");

                int enteringCol = FindEnteringColumn(tableau);
                int leavingRow = FindLeavingRow(tableau, enteringCol);

                if (leavingRow < 0)
                    return new SolveResult(Name, SolveStatus.Unbounded, iterations);

                Pivot(tableau, leavingRow, enteringCol);
                tableau.BasicVariableIndices[leavingRow - 1] = enteringCol;
                tableau.IterationNumber = iterationCount;
                iterations.Add(tableau.Clone());
            }

            if (HasPositiveArtificialInBasis(tableau))
                return new SolveResult(Name, SolveStatus.Infeasible, iterations);

            var (objectiveValue, variableValues) = DecodeSolution(model, tableau);
            return new SolveResult(Name, SolveStatus.Optimal, iterations, objectiveValue, variableValues);
        }

        /// <summary>
        /// Artificial variables start basic with an implicit -M penalty in the
        /// true objective. Since CanonicalFormBuilder leaves their objective-row
        /// entries at 0 (it's algorithm-agnostic), this sets them to +M in our
        /// "-c_j" row-0 convention, then eliminates them from row 0 so the
        /// tableau is back in proper canonical form (row 0 must be zero under
        /// every currently-basic column).
        /// </summary>
        private static void ApplyBigMPenalty(Tableau tableau)
        {
            for (int col = 0; col < tableau.VariableKinds.Count; col++)
                if (tableau.VariableKinds[col] == VariableKind.Artificial)
                    tableau[0, col] = BigM;

            for (int row = 1; row < tableau.RowCount; row++)
            {
                int basicCol = tableau.BasicVariableIndices[row - 1];
                if (tableau.VariableKinds[basicCol] != VariableKind.Artificial)
                    continue;

                double factor = tableau[0, basicCol];
                if (factor == 0) continue;

                for (int col = 0; col < tableau.ColumnCount; col++)
                    tableau[0, col] -= factor * tableau[row, col];
            }
        }

        /// <summary>Bland's rule: smallest-index column with a negative row-0 entry. Guarantees termination (no cycling) rather than chasing the "most negative" column.</summary>
        private static int FindEnteringColumn(Tableau tableau)
        {
            for (int c = 0; c < tableau.ColumnCount - 1; c++)
                if (tableau[0, c] < -Tolerance) return c;
            return -1;
        }

        /// <summary>Minimum-ratio test; ties broken by smallest basic-variable index (Bland's rule) for the same anti-cycling guarantee.</summary>
        private static int FindLeavingRow(Tableau tableau, int enteringCol)
        {
            int bestRow = -1;
            double bestRatio = double.PositiveInfinity;
            int bestBasicIndex = int.MaxValue;

            for (int row = 1; row < tableau.RowCount; row++)
            {
                double coeff = tableau[row, enteringCol];
                if (coeff <= Tolerance) continue;

                double ratio = tableau.GetRhs(row) / coeff;
                int basicIndex = tableau.BasicVariableIndices[row - 1];

                bool strictlyBetter = ratio < bestRatio - Tolerance;
                bool tiedButLowerIndex = Math.Abs(ratio - bestRatio) <= Tolerance && basicIndex < bestBasicIndex;

                if (strictlyBetter || tiedButLowerIndex)
                {
                    bestRatio = ratio;
                    bestRow = row;
                    bestBasicIndex = basicIndex;
                }
            }

            return bestRow;
        }

        private static void Pivot(Tableau tableau, int pivotRow, int pivotCol)
        {
            double pivotValue = tableau[pivotRow, pivotCol];
            for (int c = 0; c < tableau.ColumnCount; c++)
                tableau[pivotRow, c] /= pivotValue;

            for (int r = 0; r < tableau.RowCount; r++)
            {
                if (r == pivotRow) continue;
                double factor = tableau[r, pivotCol];
                if (factor == 0) continue;

                for (int c = 0; c < tableau.ColumnCount; c++)
                    tableau[r, c] -= factor * tableau[pivotRow, c];
            }
        }

        private static bool HasPositiveArtificialInBasis(Tableau tableau, double tolerance = 1e-6)
        {
            for (int row = 1; row < tableau.RowCount; row++)
            {
                int basicIndex = tableau.BasicVariableIndices[row - 1];
                if (tableau.VariableKinds[basicIndex] == VariableKind.Artificial && tableau.GetRhs(row) > tolerance)
                    return true;
            }
            return false;
        }

        private static (double objectiveValue, double[] variableValues) DecodeSolution(LPModel model, Tableau tableau)
        {
            var values = new double[model.VariableCount];
            for (int row = 1; row < tableau.RowCount; row++)
            {
                int basicIndex = tableau.BasicVariableIndices[row - 1];
                if (tableau.VariableKinds[basicIndex] == VariableKind.Decision)
                    values[basicIndex] = tableau.GetRhs(row);
            }

            double rawZ = tableau.GetRhs(0);
            // Internally everything is solved as a max problem (CanonicalFormBuilder
            // negates a min objective) - flip the sign back for a min model here.
            double objectiveValue = model.ObjectiveType == ObjectiveType.Max ? rawZ : -rawZ;

            return (objectiveValue, values);
        }
    }
}
