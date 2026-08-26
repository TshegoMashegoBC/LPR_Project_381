using System;

namespace LPR381Solver.Sensitivity
{
    public class DualityVerifier
    {
        public bool VerifyStrongDuality(
            double primal,
            double dual,
            double tolerance = 0.0001)
        {
            return
                Math.Abs(primal - dual)
                <= tolerance;
        }
    }
}
