using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JPEG.Solved.Utilities;

public static class MathEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining |
                MethodImplOptions.AggressiveOptimization)]
    public static float SumByTwoVariables(
        int from1,
        int to1,
        int from2,
        int to2,
        Func<int, int, float> function)
    {
        var sum = 0f;
        for (var value1 = from1; value1 < to1; value1++)
        {
            for (var value2 = from2; value2 < to2; value2++)
            {
                sum += function(value1, value2);
            }
        }

        return sum;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining |
                MethodImplOptions.AggressiveOptimization)]
    public static void LoopByTwoVariables(
            int from1,
            int to1,
            int from2,
            int to2,
            Action<int, int> function)
    {
        for (var value1 = from1; value1 < to1; value1++)
        {
            for (var value2 = from2; value2 < to2; value2++)
            {
                function(value1, value2);
            }
        }
    }
}
