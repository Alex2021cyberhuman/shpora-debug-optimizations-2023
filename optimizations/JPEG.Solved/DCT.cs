using System;
using System.Runtime.CompilerServices;

namespace JPEG.Solved;

public partial class DCT
{
    private const float alphaIfUIsZero = 0.70710678118f;

    public static void DCT2D(
        float[,] input,
        float[,] coeffs)
    {
        var height = input.GetLength(dimension: 0);
        var width = input.GetLength(dimension: 1);

        var beta = Beta(height: height, width: width);
        var multiplier = beta * alphaIfUIsZero * alphaIfUIsZero;
        var piDividedByDoubleWidth = MathF.PI / (2f * width);
        var piDividedByDoubleHeight = MathF.PI / (2f * height);


        var u = 0;
        var v = 0;

        // calculate first coefficient in first row then use constant twice
        CalcCoefficient();

        multiplier = beta * alphaIfUIsZero;
        // calculate other coefficients in first row and use constant once
        for (v = 1; v < width; v++)
        {
            CalcCoefficient();
        }

        // calculate coefficients in other rows 
        for (u = 1; u < height; u++)
        {
            v = 0;
            multiplier = beta * alphaIfUIsZero;
            // calculate first coefficient in row
            CalcCoefficient();

            multiplier = beta;
            for (v = 1; v < width; v++)
            {
                CalcCoefficient();
            }
        }

        void CalcCoefficient()
        {
            coeffs[u, v] = GetDctCoefficient(input: input,
                height: height,
                width: width,
                u: u,
                v: v,
                piDividedByDoubleWidth: piDividedByDoubleWidth,
                piDividedByDoubleHeight: piDividedByDoubleHeight,
                multiplier: multiplier);
        }
    }


    public static void IDCT2D(
        float[,] coeffs,
        float[,] output)
    {
        var width = coeffs.GetLength(dimension: 1);
        var height = coeffs.GetLength(dimension: 0);
        var piDividedByDoubleWidth = MathF.PI / (2f * width);
        var piDividedByDoubleHeight = MathF.PI / (2f * height);
        var beta = Beta(height: height, width: width);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
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
                           doubleYPusOne: doubleYPusOne,
                           piDividedByDoubleWidth: piDividedByDoubleWidth,
                           piDividedByDoubleHeight: piDividedByDoubleHeight) *
                       alphaIfUIsZero *
                       alphaIfUIsZero;

                for (u = 1; u < width; u++)
                {
                    sum += BasisFunction(a: coeffs[u, v],
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPusOne: doubleYPusOne,
                               piDividedByDoubleWidth: piDividedByDoubleWidth,
                               piDividedByDoubleHeight:
                               piDividedByDoubleHeight) *
                           alphaIfUIsZero;
                }

                for (v = 1; v < height; v++)
                {
                    u = 0;
                    sum += BasisFunction(a: coeffs[u, v],
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPusOne: doubleYPusOne,
                               piDividedByDoubleWidth: piDividedByDoubleWidth,
                               piDividedByDoubleHeight:
                               piDividedByDoubleHeight) *
                           alphaIfUIsZero;
                    for (u = 1; u < width; u++)
                    {
                        sum += BasisFunction(a: coeffs[u, v],
                            u: u,
                            v: v,
                            doubleXPlusOne: doubleXPlusOne,
                            doubleYPusOne: doubleYPusOne,
                            piDividedByDoubleWidth: piDividedByDoubleWidth,
                            piDividedByDoubleHeight: piDividedByDoubleHeight);
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
        int doubleYPusOne,
        float piDividedByDoubleWidth,
        float piDividedByDoubleHeight)
    {
        return a *
               MathF.Cos(x: doubleXPlusOne * u * piDividedByDoubleWidth) *
               MathF.Cos(x: doubleYPusOne * v * piDividedByDoubleHeight);
    }

    private static float Beta(
        int height,
        int width)
    {
        return 1f / width + 1f / height;
    }


    private static float GetDctCoefficient(
        float[,] input,
        int height,
        int width,
        int u,
        int v,
        float piDividedByDoubleWidth,
        float piDividedByDoubleHeight,
        float multiplier)
    {
        var sum = 0f;
        for (var y = 0; y < height; y++)
        {
            var doubleYPusOne = 2 * y + 1;
            for (var x = 0; x < width; x++)
            {
                sum += BasisFunction(a: input[x, y],
                    u: u,
                    v: v,
                    doubleXPlusOne: 2 * x + 1,
                    doubleYPusOne: doubleYPusOne,
                    piDividedByDoubleWidth: piDividedByDoubleWidth,
                    piDividedByDoubleHeight: piDividedByDoubleHeight);
            }
        }

        return sum * multiplier;
    }
}
