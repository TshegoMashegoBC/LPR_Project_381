using System;
using System.Collections.Generic;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Person D's Implementation: Non-Linear Programming bonus.
    /// Golden-section search for the minimum of a single-variable function on
    /// a bounded interval - doesn't need derivatives, so it works for any
    /// unimodal f(x), including the bonus example f(x) = x^2.
    /// </summary>
    public static class NonLinearSolver
    {
        public static NonLinearResult Minimize(Func<double, double> f, double lower, double upper, double tolerance = 1e-5)
        {
            if (lower >= upper)
                throw new ArgumentException("Lower bound must be less than upper bound.");

            const double goldenRatio = 0.6180339887; // (sqrt(5) - 1) / 2

            double a = lower;
            double b = upper;
            double c = b - goldenRatio * (b - a);
            double d = a + goldenRatio * (b - a);

            var steps = new List<NonLinearStep>();
            int iteration = 0;

            while (Math.Abs(b - a) > tolerance)
            {
                iteration++;

                if (f(c) < f(d))
                    b = d;
                else
                    a = c;

                c = b - goldenRatio * (b - a);
                d = a + goldenRatio * (b - a);

                double midpoint = (a + b) / 2;
                steps.Add(new NonLinearStep(iteration, midpoint, f(midpoint)));
            }

            double xStar = (a + b) / 2;
            return new NonLinearResult(xStar, f(xStar), steps);
        }

        // Demo for the video: minimises f(x) = x^2 on [-10, 10], which has a
        // known minimum at x = 0, f(x) = 0.
        public static void RunDemo()
        {
            Console.WriteLine("Non-linear bonus: minimising f(x) = x^2 on [-10, 10]");
            var result = Minimize(x => x * x, -10, 10);

            foreach (var step in result.Steps)
                Console.WriteLine($"Iteration {step.Iteration}: x = {step.X:0.0000}, f(x) = {step.Fx:0.0000}");

            Console.WriteLine($"Minimum found at x = {result.X:0.0000}, f(x) = {result.Fx:0.0000}");
        }
    }

    public readonly struct NonLinearStep
    {
        public int Iteration { get; }
        public double X { get; }
        public double Fx { get; }

        public NonLinearStep(int iteration, double x, double fx)
        {
            Iteration = iteration;
            X = x;
            Fx = fx;
        }
    }

    public class NonLinearResult
    {
        public double X { get; }
        public double Fx { get; }
        public IReadOnlyList<NonLinearStep> Steps { get; }

        public NonLinearResult(double x, double fx, IReadOnlyList<NonLinearStep> steps)
        {
            X = x;
            Fx = fx;
            Steps = steps;
        }
    }
}
