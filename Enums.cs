namespace LPR381Solver.Core
{
    public enum ObjectiveType { Max, Min }
    public enum Relation { LessOrEqual, GreaterOrEqual, Equal }
    public enum SignRestriction { Positive, Negative, Unrestricted, Integer, Binary }
    public enum VariableKind { Decision, Slack, Excess, Artificial, RHS }
    public enum SolveStatus { Optimal, Infeasible, Unbounded }
}