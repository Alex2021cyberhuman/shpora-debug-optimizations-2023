using System;
using System.Runtime.CompilerServices;
using JPEG.Solved.Utilities;

namespace JPEG.Solved;

public class DCT
{
    public static float[,] DCT2D(
        float[,] input)
    {
        var height = input.GetLength(0);
        var width = input.GetLength(1);
        var coeffs = new float[width, height];

        MathEx.LoopByTwoVariables(0, width, 0, height, (
            u,
            v) =>
        {
            var sum = MathEx.SumByTwoVariables(0, width, 0, height, (
                x,
                y) => BasisFunction(input[x, y], u, v, x, y, height, width));

            coeffs[u, v] = sum * Beta(height, width) * Alpha(u) * Alpha(v);
        });

        return coeffs;
    }

    public static void IDCT2D(
        float[,] coeffs,
        float[,] output)
    {
        var lengthColumns = coeffs.GetLength(1);
        var lengthLines = coeffs.GetLength(0);
        for (var y = 0; y < lengthLines; y++)
        {
            for (var x = 0; x < lengthColumns; x++)
            {
                var sum = MathEx.SumByTwoVariables(0, lengthColumns, 0,
                    lengthLines, (
                            u,
                            v) => BasisFunction(a: coeffs[u, v], u: u, v: v,
                                      x: x,
                                      y: y, height: lengthLines,
                                      width: lengthColumns) *
                                  Alpha(u) *
                                  Alpha(v));

                output[x, y] = sum * Beta(lengthLines, lengthColumns);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining |
                MethodImplOptions.AggressiveOptimization)]
    public static float BasisFunction(
        float a,
        float u,
        float v,
        float x,
        float y,
        int height,
        int width)
    {
        return a *
               MathF.Cos((2f * x + 1f) * u * MathF.PI / (2f * width)) *
               MathF.Cos((2f * y + 1f) * v * MathF.PI / (2f * height));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining |
                MethodImplOptions.AggressiveOptimization)]
    private static float Alpha(
        int u)
    {
        const float alphaIfUIsZero = 0.70710678118f;
        return u == 0 ? alphaIfUIsZero : 1f;
    }

    private static float Beta(
        int height,
        int width)
    {
        return 1f / width + 1f / height;
    }
}
