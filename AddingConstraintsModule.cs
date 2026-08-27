#nullable disable
using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class AddingConstraintsModule
    {
        public Tableau AddConstraint(Tableau optimalTableau, Constraint newConstraint)
        {
            int oldRows = optimalTableau.RowCount;
            int oldCols = optimalTableau.ColumnCount;
            
            double[,] newMatrix = new double[oldRows + 1, oldCols + 1];

            for (int r = 0; r < oldRows; r++)
            {
                for (int c = 0; c < oldCols - 1; c++) 
                    newMatrix[r, c] = optimalTableau.Matrix[r, c];
                
                newMatrix[r, oldCols] = optimalTableau.Matrix[r, oldCols - 1]; 
            }

            int newRowIdx = oldRows;
            int newVarIdx = oldCols - 1;

            for (int c = 0; c < newConstraint.Coefficients.Length; c++)
            {
                newMatrix[newRowIdx, c] = newConstraint.Coefficients[c];
            }
            newMatrix[newRowIdx, oldCols] = newConstraint.Rhs;

            double newVarCoeff = newConstraint.Relation == Relation.LessOrEqual ? 1.0 : -1.0;
            newMatrix[newRowIdx, newVarIdx] = newVarCoeff;

            var newBasicIndices = new List<int>(optimalTableau.BasicVariableIndices) { newVarIdx };
            var newNames = new List<string>(optimalTableau.VariableNames) { $"s{newRowIdx}" };
            var newKinds = new List<VariableKind>(optimalTableau.VariableKinds) 
            { 
                newConstraint.Relation == Relation.LessOrEqual ? VariableKind.Slack : VariableKind.Excess 
            };

            for (int c = 0; c < oldCols - 1; c++)
            {
                int basicRowIndex = optimalTableau.BasicVariableIndices.IndexOf(c);
                if (basicRowIndex >= 0) 
                {
                    int actualRow = basicRowIndex + 1; 
                    double factor = newMatrix[newRowIdx, c];

                    if (Math.Abs(factor) > 1e-6)
                    {
                        for (int col = 0; col <= oldCols; col++)
                        {
                            newMatrix[newRowIdx, col] -= factor * newMatrix[actualRow, col]; //[cite: 1]
                        }
                    }
                }
            }

            if (newMatrix[newRowIdx, newVarIdx] < 0)
            {
                for (int col = 0; col <= oldCols; col++)
                {
                    newMatrix[newRowIdx, col] *= -1; //[cite: 1]
                }
            }

            return new Tableau(newMatrix, newBasicIndices, newNames, newKinds, optimalTableau.IterationNumber + 1);
        }
    }
}
#nullable restore