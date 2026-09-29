using System;

namespace Dwsg.Shared.Economy
{
    public static class MonarchRules
    {
        public static double RequiredExperience(float level)
        {
            // Unity Mathf.Pow calls double Math.Pow then casts to float; MathF.Pow differs at level 62.
            return (float)Math.Round(0.000261685957f * (float)Math.Pow(level, 6f) - 0.0230197739f * (float)Math.Pow(level, 5f)
                + 0.794727564f * (float)Math.Pow(level, 4f) - 13.1911077f * (float)Math.Pow(level, 3f)
                + 126.095367f * (float)Math.Pow(level, 2f) - 168.6316f * level + 523.2616f);
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
