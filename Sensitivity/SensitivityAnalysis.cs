using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class SensitivityAnalysis
    {
        private readonly ShadowPriceCalculator
            shadowPrices = new();

        private readonly RHSAnalysis
            rhsAnalysis = new();

        private readonly VariableRangeAnalysis
            variableAnalysis = new();

        public Dictionary<string, double>
            GetShadowPrices(Tableau tableau)
        {
            return
                shadowPrices.Calculate(
                    tableau);
        }

        public (double Lower, double Upper)
            GetVariableRange(
                Tableau tableau,
                int column)
        {
            return
                variableAnalysis.GetRange(
                    tableau,
                    column);
        }
    }
}
