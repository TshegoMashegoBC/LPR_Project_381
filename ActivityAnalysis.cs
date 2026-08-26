using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class ActivityAnalysis
    {
        public LPModel AddActivity(
            LPModel model,
            double objectiveCoefficient)
        {
            LPModel clone = model.Clone();

            return clone;
        }
    }
}