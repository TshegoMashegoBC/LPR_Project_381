using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class SensitivityAnalysis
    {
        private readonly ShadowPriceCalculator
            _shadowPriceCalculator =
                new();

        private readonly RHSAnalysis
            _rhsAnalysis =
                new();

        private readonly VariableRangeAnalysis
            _rangeAnalysis =
                new();

        public Dictionary<string, double>
            GetShadowPrices(Tableau tableau)
        {
            return
                _shadowPriceCalculator
                .Calculate(tableau);
        }

        public (double Lower, double Upper)
            GetVariableRange(
                Tableau tableau,
                int column)
        {
            return
                _rangeAnalysis
                .GetSimpleRange(
                    tableau,
                    column);
        }
    }
}
