using System.Globalization;
using System.IO;
using System.Text;
using LPR381Solver.Core;

namespace LPR381Solver.IO
{
    /// <summary>
    /// Writes a solved model out to the output file exactly as the brief
    /// specifies: canonical form, every tableau iteration of whichever
    /// algorithm was used, and the final result - all numeric values rounded
    /// to 3 decimals via Tableau.ToFormattedString(), and always formatted
    /// with InvariantCulture so the file looks the same regardless of whose
    /// machine (student's or marker's) produced or opened it.
    /// </summary>
    public static class OutputWriter
    {
        public static void WriteResult(string path, LPModel model, SolveResult result)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== Model (as entered) ===");
            sb.AppendLine(model.ToString());
            sb.AppendLine();

            sb.AppendLine($"=== Algorithm: {result.AlgorithmName} ===");
            sb.AppendLine();

            sb.AppendLine("=== Canonical Form / Tableau Iterations ===");
            foreach (var tableau in result.Iterations)
                sb.AppendLine(tableau.ToFormattedString());

            sb.AppendLine("=== Result ===");
            sb.AppendLine($"Status: {result.Status}");

            if (result.Status == SolveStatus.Optimal && result.ObjectiveValue.HasValue && result.VariableValues != null)
            {
                sb.AppendLine($"Objective value: {result.ObjectiveValue.Value.ToString("0.000", CultureInfo.InvariantCulture)}");
                for (int i = 0; i < result.VariableValues.Length; i++)
                    sb.AppendLine($"x{i + 1} = {result.VariableValues[i].ToString("0.000", CultureInfo.InvariantCulture)}");
            }

            File.WriteAllText(path, sb.ToString());
        }
    }
}
