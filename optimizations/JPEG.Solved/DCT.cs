using System;
using System.Runtime.CompilerServices;
using JPEG.Solved.Processor;

namespace JPEG.Solved;

public partial class DCT
{
    private const int BlockSize = BoostedJpegProcessor.BlockSize;

    private const float beta = 1f / BlockSize + 1f / BlockSize;

    private const float piDividedByDoubleWidth = MathF.PI / (2f * BlockSize);

    private const float piDividedByDoubleHeight = MathF.PI / (2f * BlockSize);

    private const float alphaIfUIsZero = 0.70710678118f;

    public static void DCT2D(
        float[,] input,
        float[,] coeffs)
    {
        var multiplier = beta * alphaIfUIsZero * alphaIfUIsZero;
        var u = 0;
        var v = 0;

        // calculate first coefficient in first row then use constant twice
        CalcCoefficient();

        multiplier = beta * alphaIfUIsZero;
        // calculate other coefficients in first row and use constant once
        for (v = 1; v < BlockSize; v++)
        {
            CalcCoefficient();
        }

        // calculate coefficients in other rows 
        for (u = 1; u < BlockSize; u++)
        {
            v = 0;
            multiplier = beta * alphaIfUIsZero;
            // calculate first coefficient in row
            CalcCoefficient();

            multiplier = beta;
            for (v = 1; v < BlockSize; v++)
            {
                CalcCoefficient();
            }
        }

        void CalcCoefficient()
        {
            coeffs[u, v] = GetDctCoefficient(input: input,
                u: u,
                v: v,
                multiplier: multiplier);
        }
    }


    public static void IDCT2D(
        float[,] coeffs,
        float[,] output)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            for (var x = 0; x < BlockSize; x++)
            {
                var sum = 0f;
                var v = 0;
                var u = 0;

                var doubleXPlusOne = 2 * x + 1;
                var doubleYPusOne = 2 * y + 1;
                sum += BasisFunction(a: coeffs[u, v],
                           u: u,
                           v: v,
                           doubleXPlusOne: doubleXPlusOne,
                           doubleYPlusOne: doubleYPusOne) *
                       alphaIfUIsZero *
                       alphaIfUIsZero;

                for (u = 1; u < BlockSize; u++)
                {
                    sum += BasisFunction(a: coeffs[u, v],
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPlusOne: doubleYPusOne) *
                           alphaIfUIsZero;
                }

                for (v = 1; v < BlockSize; v++)
                {
                    u = 0;
                    sum += BasisFunction(a: coeffs[u, v],
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPlusOne: doubleYPusOne) *
                           alphaIfUIsZero;
                    for (u = 1; u < BlockSize; u++)
                    {
                        sum += BasisFunction(a: coeffs[u, v],
                            u: u,
                            v: v,
                            doubleXPlusOne: doubleXPlusOne,
                            doubleYPlusOne: doubleYPusOne);
                    }
                }

                output[x, y] = sum * beta;
            }
        }
    }

    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public static float BasisFunction(
        float a,
        int u,
        int v,
        int doubleXPlusOne,
        int doubleYPlusOne)
    {
        return a *
               MathF.Cos(x: doubleXPlusOne * u * piDividedByDoubleWidth) *
               MathF.Cos(x: doubleYPlusOne * v * piDividedByDoubleHeight);
    }

    private static float GetDctCoefficient(
        float[,] input,
        int u,
        int v,
        float multiplier)
    {
        var sum = 0f;
        for (var x = 0; x < BlockSize; x++)
        {
            var doubleXPlusOne = 2 * x + 1;
            for (var y = 0; y < BlockSize; y++)
            {
                sum += BasisFunction(a: input[x, y],
                    u: u,
                    v: v,
                    doubleXPlusOne: doubleXPlusOne,
                    doubleYPlusOne: 2 * y + 1);
            }
        }

        return sum * multiplier;
    }
}
