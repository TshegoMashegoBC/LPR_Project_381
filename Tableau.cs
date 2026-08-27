using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LPR381Solver.Core
{
    public class Tableau
    {
        public double[,] Matrix { get; private set; }
        public List<int> BasicVariableIndices { get; private set; }
        public List<string> VariableNames { get; }
        public List<VariableKind> VariableKinds { get; }
        public int IterationNumber { get; set; }

        public int RowCount => Matrix.GetLength(0);
        public int ColumnCount => Matrix.GetLength(1);

        public Tableau(double[,] matrix, List<int> basicVariableIndices, List<string> variableNames, List<VariableKind> variableKinds, int iterationNumber = 0)
        {
            Matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
            BasicVariableIndices = basicVariableIndices ?? throw new ArgumentNullException(nameof(basicVariableIndices));
            VariableNames = variableNames ?? throw new ArgumentNullException(nameof(variableNames));
            VariableKinds = variableKinds ?? throw new ArgumentNullException(nameof(variableKinds));
            IterationNumber = iterationNumber;
        }

        public Tableau Clone()
        {
            var copy = (double[,])Matrix.Clone();
            return new Tableau(copy, new List<int>(BasicVariableIndices), new List<string>(VariableNames), new List<VariableKind>(VariableKinds), IterationNumber);
        }

        public double this[int row, int col]
        {
            get => Matrix[row, col];
            set => Matrix[row, col] = value;
        }

        public double GetRhs(int row) => Matrix[row, ColumnCount - 1];

        public bool IsOptimalForMax(double tolerance = 1e-9)
        {
            for (int c = 0; c < ColumnCount - 1; c++)
                if (Matrix[0, c] < -tolerance) return false;
            return true;
        }

        public string ToFormattedString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"--- Iteration {IterationNumber} ---");
            sb.AppendLine(string.Join("\t", VariableNames.Concat(new[] { "RHS" })));

            for (int r = 0; r < RowCount; r++)
            {
                var rowLabel = r == 0 ? "z" : VariableNames[BasicVariableIndices[r - 1]];
                var values = Enumerable.Range(0, ColumnCount).Select(c => Matrix[r, c].ToString("0.000", CultureInfo.InvariantCulture));
                sb.AppendLine($"{rowLabel}\t" + string.Join("\t", values));
            }
            return sb.ToString();
        }
    }
}