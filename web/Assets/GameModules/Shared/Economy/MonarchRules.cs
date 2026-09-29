using System;
using System.Runtime.CompilerServices;

namespace Dwsg.Shared.Economy
{
    public static class MonarchRules
    {
        public static double RequiredExperience(float level)
        {
            // Keep Mathf's float method boundaries as well as its double Pow/Round implementations.
            return Round(0.000261685957f * Pow(level, 6f) - 0.0230197739f * Pow(level, 5f)
                + 0.794727564f * Pow(level, 4f) - 13.1911077f * Pow(level, 3f)
                + 126.095367f * Pow(level, 2f) - 168.6316f * level + 523.2616f);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Pow(float value, float power) { return (float)Math.Pow(value, power); }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Round(float value) { return (float)Math.Round(value); }

        public static void GrantExperience(double gained, ref double prestige, ref float level)
        {
            prestige += gained;
            double maximum = RequiredExperience(99f);
            if (prestige > maximum) prestige = maximum - 1.0;
            for (int candidate = 99; candidate > 0; candidate--)
                if (prestige >= RequiredExperience(candidate - 1))
                {
                    level = candidate;
                    return;
                }
        }
    }
}
