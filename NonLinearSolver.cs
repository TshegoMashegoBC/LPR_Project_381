using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class NonLinearSolver : IAlgorithm
    {
        public string Name => "Non-Linear Gradient Descent (f(x) = x²)";

        public SolveResult Solve(LPModel model)
        {
            var iterations = new List<Tableau>();
            
            double x = 10.0; // Arbitrary starting point
            double learningRate = 0.1;
            double tolerance = 1e-6;
            int maxIterations = 100;
            int step = 0;

            while (step < maxIterations)
            {
                double fx = x * x;
                double derivative = 2 * x;

                // Formats the state into a 1-row Tableau to satisfy the OutputWriter's matrix rules
                double[,] matrix = new double[1, 2] { { x, fx } };
                var basicIndices = new List<int>(); 
                var names = new List<string> { "x_val" }; 
                var kinds = new List<VariableKind> { VariableKind.Decision };

                iterations.Add(new Tableau(matrix, basicIndices, names, kinds, step));

                // Terminate when the slope flattens out (reaches the minimum)
                if (Math.Abs(derivative) < tolerance)
                    break;

                // Move x in the opposite direction of the gradient
                x = x - (learningRate * derivative);
                step++;
            }

            double finalFx = x * x;
            return new SolveResult(Name, SolveStatus.Optimal, iterations, finalFx, new double[] { x });
        }
    }
}
