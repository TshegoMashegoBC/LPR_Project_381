using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class DualSimplexAlgorithm : IAlgorithm
    {
        public string Name => "Dual Simplex Algorithm";
        private const double Tolerance = 1e-6;
        private const int MaxIterations = 500;

        public SolveResult Solve(LPModel model)
        {
            var tableau = CanonicalFormBuilder.BuildInitialTableau(model);
            var iterations = new List<Tableau> { tableau.Clone() };
            int iterationCount = 0;

            while (HasNegativeRhs(tableau)) //[cite: 2]
            {
                if (++iterationCount > MaxIterations)
                    throw new InvalidOperationException("Exceeded max iterations. Possible cycling.");

                int leavingRow = FindLeavingRow(tableau); //[cite: 2]
                if (leavingRow == -1) break; 

                int enteringCol = FindEnteringColumn(tableau, leavingRow); //[cite: 2]
                if (enteringCol == -1)
                    return new SolveResult(Name, SolveStatus.Infeasible, iterations); 

                Pivot(tableau, leavingRow, enteringCol);
                tableau.BasicVariableIndices[leavingRow - 1] = enteringCol;
                tableau.IterationNumber = iterationCount;
                iterations.Add(tableau.Clone());
            }

            var (objectiveValue, variableValues) = DecodeSolution(model, tableau);
            return new SolveResult(Name, SolveStatus.Optimal, iterations, objectiveValue, variableValues);
        }

        private static bool HasNegativeRhs(Tableau tableau)
        {
            for (int r = 1; r < tableau.RowCount; r++)
                if (tableau.GetRhs(r) < -Tolerance) return true;
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
                if (pivotVal < -Tolerance)
                {
                    double ratio = Math.Abs(tableau[0, c] / pivotVal); //[cite: 2]
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
            return (model.ObjectiveType == ObjectiveType.Max ? rawZ : -rawZ, values);
        }
    }
}