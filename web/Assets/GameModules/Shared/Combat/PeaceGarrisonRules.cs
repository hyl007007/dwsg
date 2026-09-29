using System;

namespace Dwsg.Shared.Combat
{
    public static class PeaceGarrisonRules
    {
        public static long TravelSeconds(double x, double y, int targetX, int targetY)
        {
            double distance = Math.Abs(x - targetX) + Math.Abs(y - targetY);
            if (double.IsNaN(distance) || double.IsInfinity(distance) || distance < 0 || distance > int.MaxValue * 2.0) return -1;
            return Math.Max(10, (long)Math.Ceiling(distance * 10));
        }
        public static int Capacity(int scale) { return scale == 0 ? 1000 : scale == 1 ? 2000 : scale == 2 ? 3000 : scale == 3 ? 5000 : scale == 4 ? 8000 : 0; }
    }
}
