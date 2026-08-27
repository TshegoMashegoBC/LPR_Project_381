using System;
using System.Collections.Generic;
using System.Linq;
using LPR381Solver.Core;

namespace LPR381Solver.Algorithms
{
    public class BranchAndBoundKnapsackAlgorithm : IAlgorithm
    {
        public string Name => "Branch & Bound Knapsack Algorithm";
        private const double Tolerance = 1e-6;

        private class Node
        {
            public int Level;
            public double Weight;
            public double Profit;
            public double Bound;
            public int[] Assignment = Array.Empty<int>(); 
        }

        public SolveResult Solve(LPModel model)
        {
            ValidateKnapsackModel(model);

            int n = model.VariableCount;
            double[] weights = model.Constraints[0].Coefficients;
            double capacity = model.Constraints[0].Rhs;
            double[] profits = model.ObjectiveCoefficients;

            var iterations = new List<Tableau>();
            if (capacity < 0) return new SolveResult(Name, SolveStatus.Infeasible, iterations);

            int[] order = Enumerable.Range(0, n).OrderByDescending(i => profits[i] / weights[i]).ToArray();
            double bestProfit = 0; 
            int[] bestAssignment = new int[n];

            var root = new Node
            {
                Level = 0, Weight = 0, Profit = 0,
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

                if (node.Bound <= bestProfit + Tolerance) continue;
                if (node.Level == n) continue; 

                int itemIndex = order[node.Level];

                if (node.Weight + weights[itemIndex] <= capacity + Tolerance) //[cite: 7]
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

                var excludeNode = CloneNode(node); //[cite: 7]
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
            Level = source.Level, Weight = source.Weight, Profit = source.Profit,
            Bound = source.Bound, Assignment = (int[])source.Assignment.Clone()
        };

        private static Tableau BuildTableau(Node node, int[] order, double[] weights, double[] profits, double capacity, int n, int iterationNumber)
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
            if (model.ConstraintCount != 1 || model.Constraints[0].Relation != Relation.LessOrEqual || model.ObjectiveType != ObjectiveType.Max)
                throw new ModelValidationException("Knapsack requires a single <= capacity constraint and maximization.");
            if (!model.SignRestrictions.All(s => s == SignRestriction.Binary))
                throw new ModelValidationException("Knapsack requires binary (bin) restrictions on all variables."); //[cite: 7]
        }
    }
}