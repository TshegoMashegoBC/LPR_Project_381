using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381Solver.Core
{
    /// <summary>
    /// Converts an LPModel (the model *as entered* - the brief is explicit that
    /// the input file must not already be in canonical/relaxed form) into the
    /// standard max / &lt;=-style canonical form and an initial Tableau.
    ///
    /// This is the one place canonical-form logic lives, so every algorithm and
    /// the output writer's "Canonical Form" section all agree on the same
    /// transformation instead of each re-deriving it slightly differently.
    ///
    /// Scope note for the team: this builds the *initial* tableau (standard
    /// form + slack/surplus/artificial columns). It does not run Big-M or
    /// two-phase to drive artificial variables out - that pivoting logic is
    /// algorithm-specific and belongs to whoever implements Primal Simplex.
    /// </summary>
    public static class CanonicalFormBuilder
    {
        public static Tableau BuildInitialTableau(LPModel model)
        {
            model.Validate();

            int n = model.VariableCount;
            int m = model.ConstraintCount;

            // Standardize the objective to max form (min -> max by negating),
            // so every algorithm can assume "maximize" and IsOptimalForMax applies.
            double sign = model.ObjectiveType == ObjectiveType.Max ? 1.0 : -1.0;
            var objective = model.ObjectiveCoefficients.Select(c => c * sign).ToArray();

            // Work out how many extra columns (slack/surplus/artificial) each
            // constraint needs, in order, before allocating the matrix.
            var extraColumns = new List<(VariableKind kind, int constraintRow)>();
            foreach (var (constraint, row) in model.Constraints.Select((c, i) => (c, i)))
            {
                switch (constraint.Relation)
                {
                    case Relation.LessOrEqual:
                        extraColumns.Add((VariableKind.Slack, row));
                        break;
                    case Relation.GreaterOrEqual:
                        extraColumns.Add((VariableKind.Surplus, row));
                        extraColumns.Add((VariableKind.Artificial, row));
                        break;
                    case Relation.Equal:
                        extraColumns.Add((VariableKind.Artificial, row));
                        break;
                    default:
                        throw new InvalidOperationException($"Unhandled relation: {constraint.Relation}");
                }
            }

            int totalColumns = n + extraColumns.Count + 1; // +1 for RHS
            var matrix = new double[m + 1, totalColumns];

            // Objective row: -c_j under each decision variable column, so that
            // (once optimal) row 0's RHS entry reads off as the objective value.
            for (int j = 0; j < n; j++)
                matrix[0, j] = -objective[j];

            // Decision variable coefficients, one constraint row at a time.
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    matrix[i + 1, j] = model.Constraints[i].Coefficients[j];

            var variableNames = new List<string>(Enumerable.Range(1, n).Select(i => $"x{i}"));
            var variableKinds = new List<VariableKind>(Enumerable.Repeat(VariableKind.Decision, n));
            var basicVariableIndices = new List<int>(new int[m]);

            int col = n;
            int slackCount = 0, surplusCount = 0, artificialCount = 0;

            foreach (var (kind, row) in extraColumns)
            {
                string name;
                double coefficientInOwnRow;

                switch (kind)
                {
                    case VariableKind.Slack:
                        name = $"s{++slackCount}";
                        coefficientInOwnRow = 1.0;
                        basicVariableIndices[row] = col; // slack is basic immediately for <=
                        break;
                    case VariableKind.Surplus:
                        name = $"e{++surplusCount}"; // "excess"/surplus variable
                        coefficientInOwnRow = -1.0;
                        break;
                    case VariableKind.Artificial:
                        name = $"a{++artificialCount}";
                        coefficientInOwnRow = 1.0;
                        basicVariableIndices[row] = col; // artificial is basic immediately for >= and =
                        break;
                    default:
                        throw new InvalidOperationException($"Unhandled variable kind: {kind}");
                }

                matrix[row + 1, col] = coefficientInOwnRow;
                variableNames.Add(name);
                variableKinds.Add(kind);
                col++;
            }

            int rhsCol = totalColumns - 1;
            for (int i = 0; i < m; i++)
                matrix[i + 1, rhsCol] = model.Constraints[i].Rhs;

            return new Tableau(matrix, basicVariableIndices, variableNames, variableKinds, iterationNumber: 0);
        }
    }
}
