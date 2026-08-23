using System;
using System.Globalization;
using System.IO;
using LPR381Solver.Algorithms;
using LPR381Solver.Core;
using LPR381Solver.IO;

namespace LPR381Solver
{
    /// <summary>
    /// The menu-driven entry point the brief asks for. Deliberately thin -
    /// all the real logic lives in Core/IO/Algorithms, so this file mostly
    /// just wires user choices to those pieces. Whoever adds a new algorithm
    /// only needs to register it in AlgorithmCatalog; this file doesn't change.
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            Console.WriteLine("=== LPR381 Solver ===");
            Console.WriteLine();

            var model = LoadModel();
            if (model == null) return; // user chose to give up during load

            ShowModelAndCanonicalForm(model);
            RunMainMenu(model);
        }

        private static LPModel? LoadModel()
        {
            while (true)
            {
                Console.Write("Enter path to input file (or 'sample' for the brief's knapsack example): ");
                var input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Please enter a file path.");
                    continue;
                }

                try
                {
                    var path = input.Equals("sample", StringComparison.OrdinalIgnoreCase)
                        ? WriteSampleFile()
                        : input;

                    return InputFileParser.ParseFile(path);
                }
                catch (InputFileParseException ex)
                {
                    Console.WriteLine($"Input file error: {ex.Message}");
                }
                catch (ModelValidationException ex)
                {
                    Console.WriteLine($"Model error: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Could not read file: {ex.Message}");
                }

                Console.Write("Try again? (y/n): ");
                if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                    return null;
            }
        }

        private static string WriteSampleFile()
        {
            var path = Path.Combine(Path.GetTempPath(), "lpr381_sample_knapsack.txt");
            File.WriteAllLines(path, new[]
            {
                "max +2 +3 +3 +5 +2 +4",
                "+11 +8 +6 +14 +10 +10 <=40",
                "bin bin bin bin bin bin"
            });
            return path;
        }

        private static void ShowModelAndCanonicalForm(LPModel model)
        {
            Console.WriteLine();
            Console.WriteLine("Model as entered:");
            Console.WriteLine(model);
            Console.WriteLine();

            Console.WriteLine("Canonical form:");
            Console.WriteLine(CanonicalFormBuilder.BuildInitialTableau(model).ToFormattedString());

            if (model.IsIntegerModel)
            {
                Console.WriteLine(
                    "Note: this model has integer/binary restrictions. The reference Primal Simplex " +
                    "below only solves the continuous LP relaxation - Branch & Bound (Person C/D) enforces integrality.");
            }
        }

        private static void RunMainMenu(LPModel model)
        {
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=== Main Menu ===");
                for (int i = 0; i < AlgorithmCatalog.All.Count; i++)
                    Console.WriteLine($"{i + 1}) {AlgorithmCatalog.All[i].Name}");
                Console.WriteLine("0) Exit");
                Console.Write("Choose an algorithm: ");

                var choice = Console.ReadLine()?.Trim();
                if (choice == "0") return;

                if (!int.TryParse(choice, out int index) || index < 1 || index > AlgorithmCatalog.All.Count)
                {
                    Console.WriteLine("Invalid choice.");
                    continue;
                }

                var algorithm = AlgorithmCatalog.All[index - 1];
                SolveResult result;
                try
                {
                    result = algorithm.Solve(model);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"'{algorithm.Name}' failed to solve this model: {ex.Message}");
                    continue;
                }

                DisplayResult(result);
                OfferToSaveOutput(model, result);
                RunSensitivityMenu();
            }
        }

        private static void DisplayResult(SolveResult result)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {result.AlgorithmName} ---");
            foreach (var tableau in result.Iterations)
                Console.WriteLine(tableau.ToFormattedString());

            Console.WriteLine($"Status: {result.Status}");

            if (result.Status == SolveStatus.Optimal && result.ObjectiveValue.HasValue && result.VariableValues != null)
            {
                Console.WriteLine($"Objective value: {result.ObjectiveValue.Value.ToString("0.000", CultureInfo.InvariantCulture)}");
                for (int i = 0; i < result.VariableValues.Length; i++)
                    Console.WriteLine($"x{i + 1} = {result.VariableValues[i].ToString("0.000", CultureInfo.InvariantCulture)}");
            }
        }

        private static void OfferToSaveOutput(LPModel model, SolveResult result)
        {
            Console.Write("Save this result to an output file? (y/n): ");
            if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                return;

            Console.Write("Output file path (default: output.txt): ");
            var path = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(path)) path = "output.txt";

            try
            {
                OutputWriter.WriteResult(path, model, result);
                Console.WriteLine($"Written to {Path.GetFullPath(path)}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Could not write output file: {ex.Message}");
            }
        }

        private static void RunSensitivityMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=== Sensitivity Analysis (Person E's module - not implemented yet) ===");

            string[] operations =
            {
                "Range of a selected Non-Basic Variable",
                "Apply a change to a selected Non-Basic Variable",
                "Range of a selected Basic Variable",
                "Apply a change to a selected Basic Variable",
                "Range of a selected constraint RHS",
                "Apply a change to a selected constraint RHS",
                "Add a new activity to the optimal solution",
                "Add a new constraint to the optimal solution",
                "Display shadow prices",
                "Duality: construct, solve, verify strong/weak duality"
            };

            foreach (var operation in operations)
                Console.WriteLine($"  - {operation}");
        }
    }
}