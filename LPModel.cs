using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR381Solver.Core
{
    public class LPModel
    {
        public ObjectiveType ObjectiveType { get; set; }
        public double[] ObjectiveCoefficients { get; }
        public List<Constraint> Constraints { get; }
        public SignRestriction[] SignRestrictions { get; }

        public int VariableCount => ObjectiveCoefficients.Length;
        public int ConstraintCount => Constraints.Count;

        public LPModel(
            ObjectiveType objectiveType,
            double[] objectiveCoefficients,
            List<Constraint> constraints,
            SignRestriction[] signRestrictions)
        {
            ObjectiveType = objectiveType;
            ObjectiveCoefficients = objectiveCoefficients ?? throw new ArgumentNullException(nameof(objectiveCoefficients));
            Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
            SignRestrictions = signRestrictions ?? throw new ArgumentNullException(nameof(signRestrictions));

            Validate();
        }

        public void Validate()
        {
            if (VariableCount == 0) throw new ModelValidationException("Model must have at least one decision variable.");
            if (Constraints.Count == 0) throw new ModelValidationException("Model must have at least one constraint.");
            if (SignRestrictions.Length != VariableCount) throw new ModelValidationException($"Expected {VariableCount} sign restrictions, got {SignRestrictions.Length}.");

            for (int i = 0; i < Constraints.Count; i++)
            {
                if (Constraints[i].Coefficients.Length != VariableCount)
                    throw new ModelValidationException($"Constraint {i + 1} has {Constraints[i].Coefficients.Length} coefficients, expected {VariableCount}.");
            }
        }

        public LPModel Clone() => new LPModel(
            ObjectiveType,
            (double[])ObjectiveCoefficients.Clone(),
            Constraints.Select(c => c.Clone()).ToList(),
            (SignRestriction[])SignRestrictions.Clone());
    }

    public class ModelValidationException : Exception
    {
        public ModelValidationException(string message) : base(message) { }
    }
}