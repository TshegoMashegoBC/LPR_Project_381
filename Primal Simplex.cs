using LPR381Solver.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Solves a Linear Programming model with the tableau (Primal) Simplex method,
    /// using the Big-M technique so it can handle &lt;=, &gt;=, and = constraints
    /// (and negative RHS values) generically, for both max and min problems.
    ///
    /// Every pivot is recorded as a TableauSnapshot so the caller can print the
    /// Canonical Form and all iterations to the output file, per the brief.
    /// </summary>
    public class PrimalSimplex
    {
        private const double BigM = 1_000_000d;
        private const double Epsilon = 1e-9;
        private const int MaxIterations = 1000;

        public static SimplexResult Solve(LPModel model)
        {
            int n = model.NumVariables;
            int m = model.Constraints.Count;

            // --- Normalise: flip any constraint with a negative RHS so RHS >= 0 ---
            var constraints = model.Constraints.Select(c =>
            {
                if (c.RHS < 0)
                {
                    string flippedRelation = c.Relation switch
                    {
                        "<=" => ">=",
                        ">=" => "<=",
                        _ => "="
                    };
                    return new Constraint
                    {
                        Coefficients = c.Coefficients.Select(v => -v).ToArray(),
                        Relation = flippedRelation,
                        RHS = -c.RHS
                    };
                }
                return c;
            }).ToList();

            // --- Build column layout: x1..xn, then slack/surplus (one per constraint), then artificials ---
            var colLabels = new List<string>();
            for (int j = 0; j < n; j++) colLabels.Add($"x{j + 1}");

            int[] slackSurplusCol = new int[m];
            for (int i = 0; i < m; i++)
            {
                if (constraints[i].Relation == "<=")
                {
                    colLabels.Add($"s{i + 1}");
                    slackSurplusCol[i] = colLabels.Count - 1;
                }
                else if (constraints[i].Relation == ">=")
                {
                    colLabels.Add($"e{i + 1}");
                    slackSurplusCol[i] = colLabels.Count - 1;
                }
                else
                {
                    slackSurplusCol[i] = -1; // "=" constraints have no slack/surplus
                }
            }

            int[] artificialCol = new int[m];
            for (int i = 0; i < m; i++)
            {
                if (constraints[i].Relation is ">=" or "=")
                {
                    colLabels.Add($"a{i + 1}");
                    artificialCol[i] = colLabels.Count - 1;
                }
                else
                {
                    artificialCol[i] = -1;
                }
            }

            int totalCols = colLabels.Count; // excludes RHS
            double[,] table = new double[m + 1, totalCols + 1];

            // --- Objective row (row 0). Internally always MAXIMISE; negate at the end if original was "min". ---
            double[] cInternal = model.IsMax
                ? model.ObjectiveCoefficients
                : model.ObjectiveCoefficients.Select(c => -c).ToArray();

            for (int j = 0; j < n; j++) table[0, j] = -cInternal[j];
            for (int i = 0; i < m; i++)
                if (artificialCol[i] != -1) table[0, artificialCol[i]] = BigM;

            // --- Constraint rows ---
            for (int i = 0; i < m; i++)
            {
                var c = constraints[i];
                for (int j = 0; j < n; j++) table[i + 1, j] = c.Coefficients[j];

                if (c.Relation == "<=")
                {
                    table[i + 1, slackSurplusCol[i]] = 1;
                }
                else if (c.Relation == ">=")
                {
                    table[i + 1, slackSurplusCol[i]] = -1;
                    table[i + 1, artificialCol[i]] = 1;
                }
                else // "="
                {
                    table[i + 1, artificialCol[i]] = 1;
                }

                table[i + 1, totalCols] = c.RHS;
            }

            // --- Initial basis: slack if <=, otherwise the artificial variable ---
            int[] basisCol = new int[m];
            for (int i = 0; i < m; i++)
                basisCol[i] = constraints[i].Relation == "<=" ? slackSurplusCol[i] : artificialCol[i];

            // --- Make row 0 consistent with the initial (artificial) basis: zero out their reduced cost ---
            for (int i = 0; i < m; i++)
            {
                if (constraints[i].Relation is ">=" or "=")
                {
                    for (int j = 0; j <= totalCols; j++)
                        table[0, j] -= BigM * table[i + 1, j];
                }
            }

            var basisLabels = basisCol.Select(c => colLabels[c]).ToList();
            var iterations = new List<TableauSnapshot>
            {
                Snapshot(table, colLabels, basisLabels, 0, null, null)
            };

            int iteration = 0;
            while (true)
            {
                // Entering variable: most negative coefficient in row 0 (excluding RHS)
                int enterCol = -1;
                double mostNegative = -Epsilon;
                for (int j = 0; j < totalCols; j++)
                {
                    if (table[0, j] < mostNegative)
                    {
                        mostNegative = table[0, j];
                        enterCol = j;
                    }
                }

                if (enterCol == -1) break; // optimal: no negative reduced costs remain

                // Ratio test to find leaving row
                int leaveRow = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 1; i <= m; i++)
                {
                    if (table[i, enterCol] > Epsilon)
                    {
                        double ratio = table[i, totalCols] / table[i, enterCol];
                        if (ratio < bestRatio - Epsilon)
                        {
                            bestRatio = ratio;
                            leaveRow = i;
                        }
                    }
                }

                if (leaveRow == -1)
                {
                    return new SimplexResult
                    {
                        Status = "Unbounded",
                        Iterations = iterations,
                        ColumnLabels = colLabels
                    };
                }

                string enteringLabel = colLabels[enterCol];
                string leavingLabel = basisLabels[leaveRow - 1];

                // Pivot
                double pivot = table[leaveRow, enterCol];
                for (int j = 0; j <= totalCols; j++) table[leaveRow, j] /= pivot;

                for (int i = 0; i <= m; i++)
                {
                    if (i == leaveRow) continue;
                    double factor = table[i, enterCol];
                    if (Math.Abs(factor) < Epsilon) continue;
                    for (int j = 0; j <= totalCols; j++) table[i, j] -= factor * table[leaveRow, j];
                }

                basisCol[leaveRow - 1] = enterCol;
                basisLabels[leaveRow - 1] = enteringLabel;
                iteration++;

                iterations.Add(Snapshot(table, colLabels, basisLabels, iteration, enteringLabel, leavingLabel));

                if (iteration > MaxIterations)
                    throw new InvalidOperationException("Simplex exceeded the maximum iteration limit (possible cycling).");
            }

            // --- Feasibility check: any artificial variable still basic with a positive value means infeasible ---
            for (int i = 0; i < m; i++)
            {
                if (artificialCol[i] != -1 && basisCol[i] == artificialCol[i] && table[i + 1, totalCols] > 1e-6)
                {
                    return new SimplexResult
                    {
                        Status = "Infeasible",
                        Iterations = iterations,
                        ColumnLabels = colLabels
                    };
                }
            }

            // --- Extract solution ---
            double[] full = new double[totalCols];
            for (int i = 0; i < m; i++) full[basisCol[i]] = table[i + 1, totalCols];

            double[] xSolution = full.Take(n).ToArray();
            double internalZ = table[0, totalCols];
            double objectiveValue = model.IsMax ? internalZ : -internalZ;

            return new SimplexResult
            {
                Status = "Optimal",
                Iterations = iterations,
                Solution = xSolution,
                ObjectiveValue = objectiveValue,
                FinalBasisLabels = basisLabels,
                ColumnLabels = colLabels,
                BasisColumnIndices = basisCol,
                FinalTableau = table
            };
        }

        private static TableauSnapshot Snapshot(
            double[,] table, List<string> colLabels, List<string> basisLabels,
            int iterationNumber, string? entering, string? leaving)
        {
            int rows = table.GetLength(0);
            int cols = table.GetLength(1);
            var copy = new double[rows, cols];
            Array.Copy(table, copy, table.Length);

            return new TableauSnapshot
            {
                IterationNumber = iterationNumber,
                Matrix = copy,
                ColumnLabels = new List<string>(colLabels),
                RowLabels = new List<string>(basisLabels),
                EnteringVariable = entering,
                LeavingVariable = leaving
            };
        }
    }
}
