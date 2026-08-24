using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    /// <summary>
    /// Person D's Implementation: Branch & Bound Knapsack Algorithm.
    /// Solves a 0/1 knapsack model (single <= constraint, all variables binary)
    /// by branching on "include item" / "exclude item" and fathoming with the
    /// fractional relaxation bound (classic Dantzig/greedy bound by profit-to-weight ratio).
    /// </summary>
    public class BranchAndBoundKnapsackAlgorithm : IAlgorithm
    {
        public string Name => "Branch & Bound Knapsack Algorithm (Person D)";
        private const double Tolerance = 1e-6;

        // One node = one partial decision on the items, in ratio-sorted order.
        // Level tells us how many items (in sorted order) have been decided.
        private class Node
        {
            public int Level;
            public double Weight;
            public double Profit;
            public double Bound;
            public int[] Assignment = Array.Empty<int>(); // indexed by ORIGINAL variable index: 1 in, 0 out, -1 undecided
        }

        public SolveResult Solve(LPModel model)
        {
            ValidateKnapsackModel(model);

            int n = model.VariableCount;
            double[] weights = model.Constraints[0].Coefficients;
            double capacity = model.Constraints[0].Rhs;
            double[] profits = model.ObjectiveCoefficients;

            var iterations = new List<Tableau>();

            if (capacity < 0)
                return new SolveResult(Name, SolveStatus.Infeasible, iterations);

            // Decide items best profit-to-weight ratio first - this is what makes
            // the fractional relaxation bound tight enough to fathom early.
            int[] order = Enumerable.Range(0, n)
                .OrderByDescending(i => profits[i] / weights[i])
                .ToArray();

            double bestProfit = 0; // the empty selection is always feasible
            int[] bestAssignment = new int[n];

            var root = new Node
            {
                Level = 0,
                Weight = 0,
                Profit = 0,
                Assignment = Enumerable.Repeat(-1, n).ToArray()
            };
            root.Bound = ComputeBoundDetailed(root, order, weights, profits, capacity, n).bound;

            var stack = new Stack<Node>();
            stack.Push(root);
            int iterationNumber = 0;

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                iterations.Add(BuildTableau(node, order, weights, profits, capacity, n, iterationNumber++));

                // Fathoming rule: bound cannot beat the best integer solution found so far
                if (node.Bound <= bestProfit + Tolerance)
                    continue;

                if (node.Level == n)
                    continue; // leaf - already scored below when it was created

                int itemIndex = order[node.Level];

                // Branch: include the item, but only if it still fits
                if (node.Weight + weights[itemIndex] <= capacity + Tolerance)
                {
                    var includeNode = CloneNode(node);
                    includeNode.Level++;
                    includeNode.Weight += weights[itemIndex];
                    includeNode.Profit += profits[itemIndex];
                    includeNode.Assignment[itemIndex] = 1;
                    includeNode.Bound = ComputeBoundDetailed(includeNode, order, weights, profits, capacity, n).bound;

                    if (includeNode.Level == n)
                    {
                        if (includeNode.Profit > bestProfit)
                        {
                            bestProfit = includeNode.Profit;
                            bestAssignment = (int[])includeNode.Assignment.Clone();
                        }
                    }
                    else if (includeNode.Bound > bestProfit + Tolerance)
                    {
                        stack.Push(includeNode);
                    }
                }

                // Branch: exclude the item
                var excludeNode = CloneNode(node);
                excludeNode.Level++;
                excludeNode.Assignment[itemIndex] = 0;
                excludeNode.Bound = ComputeBoundDetailed(excludeNode, order, weights, profits, capacity, n).bound;

                if (excludeNode.Level == n)
                {
                    if (excludeNode.Profit > bestProfit)
                    {
                        bestProfit = excludeNode.Profit;
                        bestAssignment = (int[])excludeNode.Assignment.Clone();
                    }
                }
                else if (excludeNode.Bound > bestProfit + Tolerance)
                {
                    stack.Push(excludeNode);
                }
            }

            var variableValues = bestAssignment.Select(v => (double)v).ToArray();
            return new SolveResult(Name, SolveStatus.Optimal, iterations, bestProfit, variableValues);
        }

        // Fractional relaxation bound: take items in ratio order, fill capacity greedily,
        // and let the last item that doesn't fully fit contribute a fractional share.
        // Also returns the fractional assignment per item so the sub-problem tableau has
        // something meaningful to display for the still-undecided items.
        private static (double bound, double[] fractional) ComputeBoundDetailed(
            Node node, int[] order, double[] weights, double[] profits, double capacity, int n)
        {
            var fractional = new double[n];
            for (int i = 0; i < n; i++)
                if (node.Assignment[i] != -1) fractional[i] = node.Assignment[i];

            double bound = node.Profit;
            double remaining = capacity - node.Weight;

            for (int i = node.Level; i < order.Length; i++)
            {
                int idx = order[i];
                if (weights[idx] <= remaining)
                {
                    remaining -= weights[idx];
                    bound += profits[idx];
                    fractional[idx] = 1.0;
                }
                else
                {
                    double portion = remaining / weights[idx];
                    bound += profits[idx] * portion;
                    fractional[idx] = portion;
                    break;
                }
            }

            return (bound, fractional);
        }

        private static Node CloneNode(Node source) => new Node
        {
            Level = source.Level,
            Weight = source.Weight,
            Profit = source.Profit,
            Bound = source.Bound,
            Assignment = (int[])source.Assignment.Clone()
        };

        // Turns a B&B node into a Tableau snapshot so it fits the shared
        // "display every sub-problem" output format used by the other algorithms.
        // Row 0 = current item values (0/1 decided, fractional for the split item, bound in RHS).
        // Row 1 = the knapsack constraint itself (weights, capacity).
        private static Tableau BuildTableau(
            Node node, int[] order, double[] weights, double[] profits, double capacity, int n, int iterationNumber)
        {
            var (bound, fractional) = ComputeBoundDetailed(node, order, weights, profits, capacity, n);

            var matrix = new double[2, n + 1];
            for (int j = 0; j < n; j++)
            {
                matrix[0, j] = fractional[j];
                matrix[1, j] = weights[j];
            }
            matrix[0, n] = bound;
            matrix[1, n] = capacity;

            var basicVariableIndices = new List<int> { node.Level < n ? order[node.Level] : n - 1 };
            var variableNames = Enumerable.Range(1, n).Select(i => $"x{i}").ToList();
            var variableKinds = Enumerable.Repeat(VariableKind.Decision, n).ToList();

            return new Tableau(matrix, basicVariableIndices, variableNames, variableKinds, iterationNumber);
        }

        private static void ValidateKnapsackModel(LPModel model)
        {
            if (model.ConstraintCount != 1)
                throw new ModelValidationException(
                    "Branch & Bound Knapsack only supports a single-constraint (0/1 knapsack) model.");

            if (model.Constraints[0].Relation != Relation.LessOrEqual)
                throw new ModelValidationException("Branch & Bound Knapsack requires a <= capacity constraint.");

            if (model.ObjectiveType != ObjectiveType.Max)
                throw new ModelValidationException("Branch & Bound Knapsack only supports maximisation.");

            if (!model.SignRestrictions.All(s => s == SignRestriction.Binary))
                throw new ModelValidationException(
                    "Branch & Bound Knapsack requires every decision variable to be binary (bin).");

            if (model.Constraints[0].Coefficients.Any(w => w <= 0))
                throw new ModelValidationException("Branch & Bound Knapsack requires strictly positive item weights.");
        }
    }
}
