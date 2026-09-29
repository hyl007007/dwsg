using System;

namespace Dwsg.Shared.Economy
{
    public static class MonarchRules
    {
        public static double RequiredExperience(float level)
        {
            // Preserve the original Mathf.Pow/Mathf.Round float intermediates and ties-to-even rounding.
            return MathF.Round(0.000261685957f * MathF.Pow(level, 6f) - 0.0230197739f * MathF.Pow(level, 5f)
                + 0.794727564f * MathF.Pow(level, 4f) - 13.1911077f * MathF.Pow(level, 3f)
                + 126.095367f * MathF.Pow(level, 2f) - 168.6316f * level + 523.2616f);
        }

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
