namespace LPR381Solver.Core
{
    /// <summary>Whether the model maximizes or minimizes the objective function.</summary>
    public enum ObjectiveType
    {
        Max,
        Min
    }

    /// <summary>The relational operator used in a constraint, as read from the input file.</summary>
    public enum Relation
    {
        LessOrEqual,    // <=
        GreaterOrEqual, // >=
        Equal           // =
    }

    /// <summary>
    /// The sign restriction placed on a decision variable, exactly as specified
    /// in the input file's final line (+, -, urs, int, bin).
    /// </summary>
    public enum SignRestriction
    {
        Positive,     // +    : x >= 0
        Negative,     // -    : x <= 0
        Unrestricted, // urs  : no restriction on sign
        Integer,      // int  : must be a whole number
        Binary        // bin  : x in {0, 1}
    }

    /// <summary>
    /// What a tableau column represents. Sensitivity analysis and the output
    /// writer both need to know this to know which operations are valid on a
    /// given column and how to label it.
    /// </summary>
    public enum VariableKind
    {
        Decision,
        Slack,
        Surplus,
        Artificial
    }
}
