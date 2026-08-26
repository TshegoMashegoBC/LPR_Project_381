using System;
using System.Collections.Generic;
using LPR381Solver.Core;

namespace LPR381Solver.Sensitivity
{
    public class ShadowPriceCalculator
    {
        public Dictionary<string, double> Calculate(Tableau tableau)
        {
            var shadowPrices = new Dictionary<string, double>();

            for (int c = 0; c < tableau.VariableNames.Count; c++)
            {
                if (tableau.VariableKinds[c] == VariableKind.Slack)
                {
                    shadowPrices[tableau.VariableNames[c]]
                        = tableau.Matrix[0, c];
                }
            }

            return shadowPrices;
        }
    }
}
