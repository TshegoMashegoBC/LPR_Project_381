using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class DualBuilder
    {
        public LPModel BuildDual(
            LPModel primal)
        {
            return primal.Clone();
        }
    }
}