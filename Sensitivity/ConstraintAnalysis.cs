using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class ConstraintAnalysis
    {
        public LPModel AddConstraint(
            LPModel model,
            Constraint newConstraint)
        {
            LPModel clone = model.Clone();

            clone.Constraints.Add(newConstraint);

            return clone;
        }

        public int GetConstraintCount(
            LPModel model)
        {
            return model.ConstraintCount;
        }
    }
}