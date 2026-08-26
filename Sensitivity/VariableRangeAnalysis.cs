namespace LPR381Solver.Sensitivity
{
    public class DualityVerifier
    {
        public bool VerifyStrongDuality(
            double primalObjective,
            double dualObjective,
            double tolerance = 0.0001)
        {
            return
                System.Math.Abs(
                    primalObjective - dualObjective)
                <= tolerance;
        }
    }
}
