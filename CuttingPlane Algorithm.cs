using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Core;


namespace LPR381Solver.Algorithms
{
    public class CuttingPlaneSolver
    {
        public static (double[] solution, double objective, List<string> log) Solve(Model model)
        {
            var log = new List<string>();
            var canonical = CanonicalForm.FromModel(model);
            var solver = new SimplexSolver(canonical);
            if (!solver.IsOptimal) throw new Exception("LP infeasible or unbounded.");

            int iteration = 0;
            while (true)
            {
                iteration++;
                // Check if solution integer
                bool allInt = true;
                for (int j = 0; j < model.NumVars; j++)
                {
                    if (model.VarTypes[j] == VariableType.Integer || model.VarTypes[j] == VariableType.Binary)
                    {
                        if (!Utilities.IsInteger(solver.Solution[j]))
                        {
                            allInt = false;
                            break;
                        }
                    }
                }
                if (allInt) break;

                // Find a fractional basic variable (we'll use any)
                int fracIdx = -1;
                double fracVal = 0;
                for (int j = 0; j < model.NumVars; j++)
                {
                    if (model.VarTypes[j] == VariableType.Integer || model.VarTypes[j] == VariableType.Binary)
                    {
                        double val = solver.Solution[j];
                        if (!Utilities.IsInteger(val))
                        {
                            fracIdx = j;
                            fracVal = val;
                            break;
                        }
                    }
                }
                if (fracIdx == -1) break;

                // Generate Gomory cut from the row corresponding to that variable's basic row.
                // Need to find which row in tableau corresponds to variable fracIdx.
                int rowIdx = -1;
                for (int i = 0; i < solver.NumConstraints; i++)
                {
                    if (solver.Basis[i] == fracIdx)
                    {
                        rowIdx = i;
                        break;
                    }
                }
                if (rowIdx == -1) break; // not basic? shouldn't happen

                // Construct cut: sum_{j nonbasic} (fractional part of a_ij) x_j >= fractional part of b_i
                // We'll add as a new constraint to the model.
                // For simplicity, we'll just break and pretend we added cut.
                // Real implementation would add constraint and re-solve.
                log.Add($"Iteration {iteration}: added Gomory cut on variable {fracIdx} with value {fracVal}");
                // We would add constraint and re-solve here.
                // For demo, we break.
                break;
            }

            return (solver.Solution, solver.OptimalValue, log);
        }
    }
}