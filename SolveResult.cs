using System.Collections.Generic;

namespace LPR381Solver.Core
{
    public class SolveResult
    {
        public string AlgorithmName { get; }
        public SolveStatus Status { get; }
        public IReadOnlyList<Tableau> Iterations { get; }
        public double? ObjectiveValue { get; }
        public double[]? VariableValues { get; }

        public SolveResult(
            string algorithmName,
            SolveStatus status,
            IReadOnlyList<Tableau> iterations,
            double? objectiveValue = null,
            double[]? variableValues = null)
        {
            AlgorithmName = algorithmName;
            Status = status;
            Iterations = iterations;
            ObjectiveValue = objectiveValue;
            VariableValues = variableValues;
        }
    }
}
