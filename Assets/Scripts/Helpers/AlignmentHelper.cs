namespace Assets.Helper
{

    public static class AlignmentHelper
    {
        public static bool IsInRange(float a, float b, float range)
        {
            return a <= b + range && a >= b - range;
        }

        public static bool IsBetween(float a, float b, float c)
        {
            return a > b && a < c;
        }

    }
}