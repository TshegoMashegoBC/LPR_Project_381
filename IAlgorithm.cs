namespace LPR381Solver.Core
{
    /// <summary>
    /// Implemented by every algorithm in the project (Primal Simplex, Revised
    /// Primal Simplex, Branch &amp; Bound Simplex, Cutting Plane, Branch &amp; Bound
    /// Knapsack). The menu only ever talks to this interface - whoever finishes
    /// an algorithm registers it in AlgorithmCatalog and the menu picks it up
    /// automatically, with no changes needed to Program.cs.
    /// </summary>
    public interface IAlgorithm
    {
        /// <summary>Display name shown in the menu.</summary>
        string Name { get; }

        /// <summary>
        /// Solves the model and returns its full iteration history plus the
        /// final status. Should not mutate the given model - clone it first
        /// if the algorithm needs to modify it (e.g. Branch &amp; Bound fixing a
        /// variable per sub-problem).
        /// </summary>
        SolveResult Solve(LPModel model);
    }
}
