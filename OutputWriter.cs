using System.IO;
using LPR381Solver.Core;

namespace LPR381Solver.IO
{
    public class OutputWriter
    {
        public static void WriteResult(string filePath, SolveResult result)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine($"Solver: {result.SolverName}");
            writer.WriteLine($"Status: {result.Status}");
            
            if (result.Status == SolveStatus.Optimal)
            {
                writer.WriteLine($"Optimal Z: {result.ObjectiveValue}");
                for (int i = 0; i < result.VariableValues.Length; i++)
                {
                    writer.WriteLine($"x{i + 1} = {result.VariableValues[i]}");
                }
            }

            writer.WriteLine("\n--- Iteration History ---");
            foreach (var iteration in result.Iterations)
            {
                writer.WriteLine(iteration.ToFormattedString());
            }
        }
    }
}
