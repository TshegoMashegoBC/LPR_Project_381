using System.Collections.Generic;

namespace LPR381Solver.Core
{
    /// <summary>
    /// What every algorithm hands back after solving a model: its full
    /// iteration history (for the output file's "display all tableau
    /// iterations" requirement), the final status, and - if optimal - the
    /// objective value and decision variable values decoded from the final
    /// tableau.
    /// </summary>
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
