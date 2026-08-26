using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class RHSAnalysis
    {
        public bool IsFeasibleAfterChange(
            Tableau tableau,
            int row,
            double newValue)
        {
            Tableau copy = tableau.Clone();

            copy.Matrix[row, copy.ColumnCount - 1]
                = newValue;

            for (int i = 1; i < copy.RowCount; i++)
            {
                if (copy.GetRhs(i) < 0)
                    return false;
            }

            return true;
        }
    }
}