using System;

namespace Mirage.KCP
{
    public static class Utils
    {
        public static int Clamp(int value, int lower, int upper)
        {
            return Math.Min(Math.Max(lower, value), upper);
        }

        public static bool Equal(ReadOnlyMemory<byte> seg1, ReadOnlyMemory<byte> seg2)
        {
            if (seg1.Length != seg2.Length)
                return false;

            for (int i = 0; i < seg1.Length; i++)
            {
                if (seg1.Span[i] != seg2.Span[i])
                    return false;
            }
            return true;
        }
    }
}
