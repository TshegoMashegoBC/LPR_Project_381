using System;
using System.Collections.Generic;

namespace LPR381Solver.Core
{
    public class SolveResult
    {
        public string SolverName { get; }
        public SolveStatus Status { get; }
        public List<Tableau> Iterations { get; }
        public double? ObjectiveValue { get; }
        public double[] VariableValues { get; }

        public SolveResult(string solverName, SolveStatus status, List<Tableau> iterations, double? objectiveValue = null, double[]? variableValues = null)
        {
            SolverName = solverName;
            Status = status;
            Iterations = iterations ?? new List<Tableau>();
            ObjectiveValue = objectiveValue;
            VariableValues = variableValues ?? Array.Empty<double>();
        }
    }
}
