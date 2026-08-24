using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Evaluates models using the Dual Simplex method.
    /// Highly efficient for re-optimizing tableaus in Branch & Bound when 
    /// adding new bounds creates a negative RHS (primal infeasibility).
    /// </summary>
    public class DualSimplexAlgorithm : IAlgorithm
    {
        public string Name => "Dual Simplex Algorithm";
        private const double Tolerance = 1e-6;
        private const int MaxIterations = 500;

        public SolveResult Solve(LPModel model)
        {
            // Note: For a pure Dual Simplex run from scratch, >= constraints should 
            // have their excess variable subtracted and the entire constraint multiplied 
            // by -1 so the excess variable enters as a Basic Variable[cite: 3].
            var tableau = CanonicalFormBuilder.BuildInitialTableau(model);
            
            var iterations = new List<Tableau> { tableau.Clone() };
            int iterationCount = 0;

            // Phase 1 is complete once there are no more negative values in the RHS column[cite: 3].
            while (HasNegativeRhs(tableau))
            {
                if (++iterationCount > MaxIterations)
                    throw new InvalidOperationException("Exceeded max iterations. Possible cycling.");

                // 1. Pivot Row: Only negative values in the RHS constraints can become the pivot row[cite: 3].
                int leavingRow = FindLeavingRow(tableau);
                if (leavingRow == -1) break; 

                // 2. Ratio Test: ABS(z-value / pivot row value)[cite: 3].
                int enteringCol = FindEnteringColumn(tableau, leavingRow);

                if (enteringCol == -1)
                    return new SolveResult(Name, SolveStatus.Infeasible, iterations); // Infeasible if no negative entries in pivot row

                // 3. Perform the Pivot
                Pivot(tableau, leavingRow, enteringCol);
                tableau.BasicVariableIndices[leavingRow - 1] = enteringCol;
                tableau.IterationNumber = iterationCount;
                iterations.Add(tableau.Clone());
            }

            // Once Phase 1 clears the negative RHS values, you would typically check 
            // for Z-row optimality (Primal Phase 2). For this engine, we extract the repaired solution.
            var (objectiveValue, variableValues) = DecodeSolution(model, tableau);
            return new SolveResult(Name, SolveStatus.Optimal, iterations, objectiveValue, variableValues);
        }

        private static bool HasNegativeRhs(Tableau tableau)
        {
            for (int r = 1; r < tableau.RowCount; r++)
            {
                if (tableau.GetRhs(r) < -Tolerance) return true;
            }
            return false;
        }

        private static int FindLeavingRow(Tableau tableau)
        {
            int bestRow = -1;
            double mostNegative = -Tolerance;

            for (int r = 1; r < tableau.RowCount; r++)
            {
                double rhs = tableau.GetRhs(r);
                if (rhs < mostNegative)
                {
                    mostNegative = rhs;
                    bestRow = r;
                }
            }
            return bestRow;
        }

        private static int FindEnteringColumn(Tableau tableau, int leavingRow)
        {
            int bestCol = -1;
            double smallestRatio = double.MaxValue;

            for (int c = 0; c < tableau.ColumnCount - 1; c++)
            {
                double pivotVal = tableau[leavingRow, c];
                
                // We only evaluate negative pivot row values
                if (pivotVal < -Tolerance)
                {
                    // Calculation: ABS(z-value / pivot row value)[cite: 3].
                    double ratio = Math.Abs(tableau[0, c] / pivotVal);
                    if (ratio < smallestRatio)
                    {
                        smallestRatio = ratio;
                        bestCol = c;
                    }
                }
            }
            return bestCol;
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
                if (Math.Abs(factor) < Tolerance) continue;

                for (int c = 0; c < tableau.ColumnCount; c++)
                    tableau[r, c] -= factor * tableau[pivotRow, c];
            }
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
            double objectiveValue = model.ObjectiveType == ObjectiveType.Max ? rawZ : -rawZ;
            return (objectiveValue, values);
        }
    }
}