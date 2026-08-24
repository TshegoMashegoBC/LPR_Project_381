using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Person C's Implementation: Branch & Bound Simplex Algorithm.
    /// Handles tree generation, backtracking, fathoming, and fractional variable selection
    /// to solve pure and mixed Integer Programming models.
    /// </summary>
    public class BranchAndBoundSimplexAlgorithm : IAlgorithm
    {
        public string Name => "Branch & Bound Simplex Algorithm (Person C)";
        private const double Tolerance = 1e-6;

        public SolveResult Solve(LPModel model)
        {
            var allIterations = new List<Tableau>();
            var subProblems = new Stack<LPModel>();
            
            // Track the best integer solution found so far (the incumbent)
            double bestIntegerZ = model.ObjectiveType == ObjectiveType.Max ? double.NegativeInfinity : double.PositiveInfinity;
            double[] bestIntegerValues = null;
            bool foundIntegerSolution = false;

            // We use the continuous Primal Simplex solver to evaluate each node's relaxation
            var continuousSolver = new PrimalSimplexAlgorithm();

            // Push the initial relaxed model as the root node
            subProblems.Push(model);

            while (subProblems.Count > 0)
            {
                var currentModel = subProblems.Pop();

                // 1. Solve the continuous LP relaxation for this sub-problem
                SolveResult result;
                try
                {
                    result = continuousSolver.Solve(currentModel);
                }
                catch
                {
                    continue; // Safely skip if the continuous solver fails mathematically
                }
                
                // Collect tableaus so the output writer displays ALL sub-problem iterations
                allIterations.AddRange(result.Iterations);

                // 2. Fathoming Rule 1: Infeasibility 
                if (result.Status == SolveStatus.Infeasible || result.Status == SolveStatus.Unbounded)
                {
                    continue; 
                }

                // 3. Fathoming Rule 2: Bounding (Worse than best-so-far)
                if (model.ObjectiveType == ObjectiveType.Max && result.ObjectiveValue <= bestIntegerZ - Tolerance)
                    continue;
                if (model.ObjectiveType == ObjectiveType.Min && result.ObjectiveValue >= bestIntegerZ + Tolerance)
                    continue;

                // 4. Candidate Selection & Integrality Check
                bool isIntegerFeasible = true;
                int branchVarIndex = -1;
                double minDistanceToHalf = double.MaxValue;
                double branchVarValue = 0;

                for (int i = 0; i < currentModel.VariableCount; i++)
                {
                    // Only enforce integrality on variables explicitly marked as Integer or Binary
                    if (currentModel.SignRestrictions[i] == SignRestriction.Integer || 
                        currentModel.SignRestrictions[i] == SignRestriction.Binary)
                    {
                        double val = result.VariableValues[i];
                        double nearestInt = Math.Round(val);
                        
                        // Check if the variable is fractional
                        if (Math.Abs(val - nearestInt) > Tolerance)
                        {
                            isIntegerFeasible = false;
                            
                            // The fraction is always positive (e.g., 2.25 -> 0.25, 2.75 -> 0.75)
                            double fraction = val - Math.Floor(val);
                            double distanceToHalf = Math.Abs(fraction - 0.5);

                            // Tie-breaker rule: Strictly less than (<) ensures the lower subscript 
                            // is chosen if distances are identical, as we iterate from x_1 to x_n.
                            if (distanceToHalf < minDistanceToHalf - Tolerance)
                            {
                                minDistanceToHalf = distanceToHalf;
                                branchVarIndex = i;
                                branchVarValue = val;
                            }
                        }
                    }
                }

                // 5. Fathoming Rule 3: Valid Integer Solution Found
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

                // 6. Sub-Problem Generation (Branching)
                // We branch on the selected variable x_k by adding x_k <= floor and x_k >= ceil
                var subProblem2 = currentModel.Clone(); 
                var subProblem1 = currentModel.Clone(); 

                double[] coeffs = new double[currentModel.VariableCount];
                coeffs[branchVarIndex] = 1.0;

                // We push Sub-problem 2 (>=) first, so that Sub-problem 1 (<=) is popped 
                // and solved first, mimicking standard Depth-First search tree structures.
                
                // Sub-Problem 2: x_k >= Ceil
                subProblem2.Constraints.Add(new Constraint(coeffs, Relation.GreaterOrEqual, Math.Ceiling(branchVarValue)));
                subProblems.Push(subProblem2);

                // Sub-Problem 1: x_k <= Floor
                subProblem1.Constraints.Add(new Constraint(coeffs, Relation.LessOrEqual, Math.Floor(branchVarValue)));
                subProblems.Push(subProblem1);
            }

            // Return the final result
            if (foundIntegerSolution)
            {
                return new SolveResult(Name, SolveStatus.Optimal, allIterations, bestIntegerZ, bestIntegerValues);
            }
            
            return new SolveResult(Name, SolveStatus.Infeasible, allIterations);
        }
    }
}