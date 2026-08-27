using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public interface IAlgorithm
    {
        string Name { get; }
        SolveResult Solve(LPModel model);
    }
}