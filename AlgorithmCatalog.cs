using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Every algorithm the team writes gets registered here, once. The menu
    /// just lists and invokes whatever's in <see cref="All"/> - nobody needs to
    /// touch Program.cs to add their algorithm, they add one line here.
    /// </summary>
    public static class AlgorithmCatalog
    {
        public static IReadOnlyList<IAlgorithm> All { get; } = new List<IAlgorithm>
        {
            new PrimalSimplexAlgorithm(),

            // Add your algorithm below once it implements IAlgorithm:
            // new RevisedPrimalSimplexAlgorithm(),   // Person B
            // new BranchAndBoundSimplexAlgorithm(),  // Person C
            // new CuttingPlaneAlgorithm(),            // Person B
            // new BranchAndBoundKnapsackAlgorithm(), // Person D
        };
    }
}
