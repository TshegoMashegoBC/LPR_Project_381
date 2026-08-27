using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class CuttingPlaneAlgorithm : IAlgorithm
    {
        public string Name => "Cutting Plane Algorithm";
        private const double Tolerance = 1e-6;
        private const int MaxCuts = 100;

        public SolveResult Solve(LPModel model)
        {
            var iterations = new List<Tableau>();
            var primalSolver = new PrimalSimplexAlgorithm();
            var constraintsModule = new AddingConstraintsModule();

            var currentResult = primalSolver.Solve(model);
            if (currentResult.Status != SolveStatus.Optimal || currentResult.Iterations.Count == 0)
                return currentResult;

            iterations.AddRange(currentResult.Iterations);
            Tableau currentTableau = currentResult.Iterations[currentResult.Iterations.Count - 1];

            int cutCount = 0;

            while (cutCount < MaxCuts)
            {
                int fractionalRow = -1;
                double maxFraction = 0;

                for (int r = 1; r < currentTableau.RowCount; r++)
                {
                    int basicVarIdx = currentTableau.BasicVariableIndices[r - 1];
                    if (basicVarIdx < model.VariableCount && 
                        (model.SignRestrictions[basicVarIdx] == SignRestriction.Integer || 
                         model.SignRestrictions[basicVarIdx] == SignRestriction.Binary))
                    {
                        double rhsVal = currentTableau.GetRhs(r);
                        double fraction = rhsVal - Math.Floor(rhsVal);

                        if (fraction > Tolerance && fraction < 1 - Tolerance)
                        {
                            if (Math.Abs(fraction - 0.5) < Math.Abs(maxFraction - 0.5) || fractionalRow == -1) //[cite: 5]
                            {
                                maxFraction = fraction;
                                fractionalRow = r;
                            }
                        }
                    }
                }

                if (fractionalRow == -1) break;

                double[] cutCoefficients = new double[currentTableau.ColumnCount - 1];
                for (int c = 0; c < currentTableau.ColumnCount - 1; c++)
                {
                    double coeff = currentTableau[fractionalRow, c];
                    double fractionalPart = coeff - Math.Floor(coeff); 
                    cutCoefficients[c] = -fractionalPart; //[cite: 5]
                }
                
                double cutRhs = -(currentTableau.GetRhs(fractionalRow) - Math.Floor(currentTableau.GetRhs(fractionalRow))); //[cite: 5]
                Constraint gomoryCut = new Constraint(cutCoefficients, Relation.LessOrEqual, cutRhs);

                currentTableau = constraintsModule.AddConstraint(currentTableau, gomoryCut); //[cite: 1]
                iterations.Add(currentTableau.Clone());

                currentTableau = RunDualSimplexOnTableau(currentTableau, iterations); //[cite: 2]
                cutCount++;
            }

            var finalResult = DecodeSolution(model, currentTableau);
            return new SolveResult(Name, SolveStatus.Optimal, iterations, finalResult.objective, finalResult.values);
        }

        private Tableau RunDualSimplexOnTableau(Tableau tableau, List<Tableau> iterations)
        {
            while (true)
            {
                int leavingRow = -1;
                double mostNegative = -Tolerance;

                for (int r = 1; r < tableau.RowCount; r++)
                {
                    double rhs = tableau.GetRhs(r);
                    if (rhs < mostNegative) { mostNegative = rhs; leavingRow = r; }
                }

                if (leavingRow == -1) break;

                int enteringCol = -1;
                double smallestRatio = double.MaxValue;

                for (int c = 0; c < tableau.ColumnCount - 1; c++)
                {
                    double pivotVal = tableau[leavingRow, c];
                    if (pivotVal < -Tolerance)
                    {
                        double ratio = Math.Abs(tableau[0, c] / pivotVal);
                        if (ratio < smallestRatio) { smallestRatio = ratio; enteringCol = c; }
                    }
                }

                if (enteringCol == -1) throw new InvalidOperationException("Dual Simplex failed during cut resolution.");

                double pivotValue = tableau[leavingRow, enteringCol];
                for (int c = 0; c < tableau.ColumnCount; c++) tableau[leavingRow, c] /= pivotValue;
                for (int r = 0; r < tableau.RowCount; r++)
                {
                    if (r == leavingRow) continue;
                    double factor = tableau[r, enteringCol];
                    if (Math.Abs(factor) < Tolerance) continue;
                    for (int c = 0; c < tableau.ColumnCount; c++) tableau[r, c] -= factor * tableau[leavingRow, c];
                }
                
                tableau.BasicVariableIndices[leavingRow - 1] = enteringCol;
                iterations.Add(tableau.Clone());
            }
            return tableau;
        }

        private (double objective, double[] values) DecodeSolution(LPModel model, Tableau tableau)
        {
            var values = new double[model.VariableCount];
            for (int row = 1; row < tableau.RowCount; row++)
            {
                int basicIndex = tableau.BasicVariableIndices[row - 1];
                if (basicIndex < model.VariableCount)
                    values[basicIndex] = tableau.GetRhs(row);
            }
            double rawZ = tableau.GetRhs(0);
            return (model.ObjectiveType == ObjectiveType.Max ? rawZ : -rawZ, values);
        }
    }
}