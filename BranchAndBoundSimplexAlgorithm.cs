using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class BranchAndBoundSimplexAlgorithm : IAlgorithm
    {
        public string Name => "Branch & Bound Simplex Algorithm";
        private const double Tolerance = 1e-6;

        public SolveResult Solve(LPModel model)
        {
            var allIterations = new List<Tableau>();
            var subProblems = new Stack<LPModel>();
            
            double bestIntegerZ = model.ObjectiveType == ObjectiveType.Max ? double.NegativeInfinity : double.PositiveInfinity;
            double[]? bestIntegerValues = null; 
            bool foundIntegerSolution = false;

            var continuousSolver = new PrimalSimplexAlgorithm();
            subProblems.Push(model);

            while (subProblems.Count > 0)
            {
                var currentModel = subProblems.Pop();

                SolveResult result;
                try { result = continuousSolver.Solve(currentModel); }
                catch { continue; }
                
                allIterations.AddRange(result.Iterations);

                if (result.Status == SolveStatus.Infeasible || result.Status == SolveStatus.Unbounded)
                    continue; 

                if (model.ObjectiveType == ObjectiveType.Max && result.ObjectiveValue <= bestIntegerZ - Tolerance)
                    continue;
                if (model.ObjectiveType == ObjectiveType.Min && result.ObjectiveValue >= bestIntegerZ + Tolerance)
                    continue;

                if (result.VariableValues == null)
                    continue;

                bool isIntegerFeasible = true;
                int branchVarIndex = -1;
                double minDistanceToHalf = double.MaxValue;
                double branchVarValue = 0;

                for (int i = 0; i < currentModel.VariableCount; i++)
                {
                    if (currentModel.SignRestrictions[i] == SignRestriction.Integer || 
                        currentModel.SignRestrictions[i] == SignRestriction.Binary)
                    {
                        double val = result.VariableValues[i];
                        double nearestInt = Math.Round(val);
                        
                        if (Math.Abs(val - nearestInt) > Tolerance)
                        {
                            isIntegerFeasible = false;
                            double fraction = val - Math.Floor(val);
                            double distanceToHalf = Math.Abs(fraction - 0.5);

                            if (distanceToHalf < minDistanceToHalf - Tolerance) //[cite: 4]
                            {
                                minDistanceToHalf = distanceToHalf;
                                branchVarIndex = i;
                                branchVarValue = val;
                            }
                        }
                    }
                }

                if (isIntegerFeasible)
                {
                    if ((model.ObjectiveType == ObjectiveType.Max && result.ObjectiveValue > bestIntegerZ) ||
                        (model.ObjectiveType == ObjectiveType.Min && result.ObjectiveValue < bestIntegerZ))
                    {
                        bestIntegerZ = result.ObjectiveValue.Value;
                        bestIntegerValues = (double[])result.VariableValues.Clone();
                        foundIntegerSolution = true;
                    }
                    continue; 
                }

                var subProblem2 = currentModel.Clone(); 
                var subProblem1 = currentModel.Clone(); 
                double[] coeffs = new double[currentModel.VariableCount];
                coeffs[branchVarIndex] = 1.0;

                subProblem2.Constraints.Add(new Constraint(coeffs, Relation.GreaterOrEqual, Math.Ceiling(branchVarValue))); //[cite: 4]
                subProblems.Push(subProblem2);

                subProblem1.Constraints.Add(new Constraint(coeffs, Relation.LessOrEqual, Math.Floor(branchVarValue))); //[cite: 4]
                subProblems.Push(subProblem1);
            }

            if (foundIntegerSolution)
                return new SolveResult(Name, SolveStatus.Optimal, allIterations, bestIntegerZ, bestIntegerValues ?? Array.Empty<double>());
            
            return new SolveResult(Name, SolveStatus.Infeasible, allIterations);
        }
    }
}