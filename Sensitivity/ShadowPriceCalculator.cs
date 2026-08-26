using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class ShadowPriceCalculator
    {
        public Dictionary<string, double> Calculate(Tableau tableau)
        {
            Dictionary<string, double> result = new();

            for (int i = 0; i < tableau.VariableNames.Count; i++)
            {
                result[tableau.VariableNames[i]] =
                    tableau.Matrix[0, i];
            }

            return result;
        }
    }
}
