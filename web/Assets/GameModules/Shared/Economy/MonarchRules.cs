using System;
using System.Runtime.CompilerServices;

namespace Dwsg.Shared.Economy
{
    public static class MonarchRules
    {
        // Complete original Unity Mathf curve for valid game levels, promoted from its float results.
        // Keeping the derived values avoids different Mono/.NET intermediate arithmetic and JSON float formatting.
        private static readonly double[] OriginalExperienceCurve =
        {
            523, 468, 597, 855, 1203, 1612, 2065, 2549, 3060, 3597,
            4162, 4760, 5396, 6075, 6801, 7578, 8410, 9298, 10243, 11244,
            12301, 13415, 14589, 15826, 17136, 18532, 20036, 21678, 23498, 25552,
            27908, 30655, 33902, 37783, 42456, 48114, 54981, 63319, 73434, 85675,
            100443, 118193, 139441, 164763, 194809, 230301, 272041, 320918, 377910, 444095,
            520653, 608873, 710164, 826054, 958205, 1108413, 1278621, 1470923, 1687574, 1930994,
            2203782, 2508721, 2848782, 3227145, 3647192, 4112526, 4626987, 5194635, 5819792, 6507035,
            7261201, 8087411, 8991069, 9977876, 11053848, 12225320, 13498938, 14881722, 16381025, 18004564,
            19760446, 21657144, 23703570, 25909002, 28283190, 30836292, 33578908, 36522144, 39677572, 43057220,
            46673656, 50539964, 54669756, 59077200, 63776976, 68784440, 74115464, 79786528, 85814768, 92217960,
        };

        public static double RequiredExperience(float level)
        {
            if (level >= 0 && level <= 99 && level == (int)level) return OriginalExperienceCurve[(int)level];
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
