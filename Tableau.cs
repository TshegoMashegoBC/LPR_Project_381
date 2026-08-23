using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LPR381Solver.Core
{
    
    public class Tableau
    {
        /// [row, col] matrix. Row 0 = objective row, rows 1..m = constraints. Last column = RHS.
        public double[,] Matrix { get; private set; }

        /// Column index of the basic variable for constraint row (index i -> row i+1's basic variable).
        public List<int> BasicVariableIndices { get; private set; }

        ///Display name for every column except RHS, in column order (e.g. x1, x2, s1, a1).
        public List<string> VariableNames { get; }

        /// What kind of variable each column represents - lets sensitivity analysis know which operations are valid on which column.
        public List<VariableKind> VariableKinds { get; }

        /// iteration of its algorithm this snapshot represents - printed in the output file so every step is traceable.
        public int IterationNumber { get; set; }

        public int RowCount => Matrix.GetLength(0);
        public int ColumnCount => Matrix.GetLength(1);

        public Tableau(
            double[,] matrix,
            List<int> basicVariableIndices,
            List<string> variableNames,
            List<VariableKind> variableKinds,
            int iterationNumber = 0)
        {
            Matrix = matrix ?? throw new ArgumentNullException(nameof(matrix));
            BasicVariableIndices = basicVariableIndices ?? throw new ArgumentNullException(nameof(basicVariableIndices));
            VariableNames = variableNames ?? throw new ArgumentNullException(nameof(variableNames));
            VariableKinds = variableKinds ?? throw new ArgumentNullException(nameof(variableKinds));
            IterationNumber = iterationNumber;

            if (BasicVariableIndices.Count != RowCount - 1)
                throw new ArgumentException("Must have exactly one basic variable per constraint row.");
            if (VariableNames.Count != ColumnCount - 1) // -1 excludes the RHS column
                throw new ArgumentException("VariableNames must cover every column except RHS.");
            if (VariableKinds.Count != VariableNames.Count)
                throw new ArgumentException("VariableKinds must match VariableNames one-for-one.");
        }

        /// <summary>
        /// Deep copy. Every simplex pivot and every "what-if" sensitivity change
        /// output file's iteration history.
        /// </summary>
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

        public double[] GetColumn(int col)
        {
            var result = new double[RowCount];
            for (int r = 0; r < RowCount; r++) result[r] = Matrix[r, col];
            return result;
        }

        public double[] GetRow(int row)
        {
            var result = new double[ColumnCount];
            for (int c = 0; c < ColumnCount; c++) result[c] = Matrix[row, c];
            return result;
        }

        /// <summary>
        /// True once no reduced cost in the objective row can still improve a
        /// max-form objective. Every simplex-family algorithm uses this (or its
        /// mirror for min) as its stopping test, so it lives here once rather
        /// than being reimplemented per algorithm.
        /// </summary>
        public bool IsOptimalForMax(double tolerance = 1e-9)
        {
            for (int c = 0; c < ColumnCount - 1; c++)
                if (Matrix[0, c] < -tolerance) return false;
            return true;
        }

        /// Renders this tableau exactly as the output file needs it: a header
        /// row of variable names, then each row labelled by its basic variable,
        /// all values rounded to 3 decimals per the brief's rounding rule.
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
