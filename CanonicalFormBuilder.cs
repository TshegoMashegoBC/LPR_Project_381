using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public static class CanonicalFormBuilder
    {
        public static Tableau BuildInitialTableau(LPModel model)
        {
            int decisionVars = model.VariableCount;
            int numConstraints = model.ConstraintCount;
            
            int slackCount = 0, excessCount = 0, artificialCount = 0;
            foreach (var c in model.Constraints)
            {
                if (c.Relation == Relation.LessOrEqual) slackCount++;
                else if (c.Relation == Relation.GreaterOrEqual) { excessCount++; artificialCount++; }
                else if (c.Relation == Relation.Equal) artificialCount++;
            }

            int totalCols = decisionVars + slackCount + excessCount + artificialCount + 1; 
            int totalRows = numConstraints + 1; 

            double[,] matrix = new double[totalRows, totalCols];
            var basicIndices = new List<int>();
            var varNames = new List<string>();
            var varKinds = new List<VariableKind>();

            for (int i = 0; i < decisionVars; i++)
            {
                matrix[0, i] = model.ObjectiveType == ObjectiveType.Max ? -model.ObjectiveCoefficients[i] : model.ObjectiveCoefficients[i];
                varNames.Add($"x{i + 1}");
                varKinds.Add(VariableKind.Decision);
            }

            int currentCol = decisionVars;
            int sCount = 1, eCount = 1, aCount = 1;

            for (int r = 0; r < numConstraints; r++)
            {
                int rowIdx = r + 1;
                var constraint = model.Constraints[r];

                for (int c = 0; c < decisionVars; c++)
                    matrix[rowIdx, c] = constraint.Coefficients[c];

                matrix[rowIdx, totalCols - 1] = constraint.Rhs;

                if (constraint.Relation == Relation.LessOrEqual)
                {
                    matrix[rowIdx, currentCol] = 1.0;
                    basicIndices.Add(currentCol);
                    varNames.Add($"s{sCount++}");
                    varKinds.Add(VariableKind.Slack);
                    currentCol++;
                }
                else if (constraint.Relation == Relation.GreaterOrEqual)
                {
                    matrix[rowIdx, currentCol] = -1.0;
                    varNames.Add($"e{eCount++}");
                    varKinds.Add(VariableKind.Excess); //[cite: 2]
                    currentCol++;

                    matrix[rowIdx, currentCol] = 1.0;
                    basicIndices.Add(currentCol);
                    varNames.Add($"a{aCount++}");
                    varKinds.Add(VariableKind.Artificial);
                    currentCol++;
                }
                else if (constraint.Relation == Relation.Equal)
                {
                    matrix[rowIdx, currentCol] = 1.0;
                    basicIndices.Add(currentCol);
                    varNames.Add($"a{aCount++}");
                    varKinds.Add(VariableKind.Artificial);
                    currentCol++;
                }
            }

            return new Tableau(matrix, basicIndices, varNames, varKinds, 0);
        }
    }
}