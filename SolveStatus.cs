namespace LPR381Solver.Core
{
    /// <summary>The outcome of running an algorithm against a model - covers the brief's "Special Case Requirements" (infeasible/unbounded detection).</summary>
    public enum SolveStatus
    {
        Optimal,
        Infeasible,
        Unbounded
    }
}
