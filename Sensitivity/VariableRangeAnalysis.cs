using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class VariableRangeAnalysis
    {
        public (double Lower, double Upper)
            GetRange(Tableau tableau, int column)
        {
            double current =
                tableau.Matrix[0, column];

            return
            (
                current - 1000,
                current + 1000
            );
        }
    }
}
