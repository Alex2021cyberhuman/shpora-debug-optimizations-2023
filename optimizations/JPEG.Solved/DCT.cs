using System;
using System.Runtime.CompilerServices;

namespace JPEG.Solved;

public class DCT
{
    private const float alphaIfUIsZero = 0.70710678118f;

    public static float[,] DCT2D(
        float[,] input)
    {
        var height = input.GetLength(dimension: 0);
        var width = input.GetLength(dimension: 1);
        var coeffs = new float[width, height];

        var beta = Beta(height: height, width: width);
        var piDividedByDoubleWidth = MathF.PI / (2f * width);
        var piDividedByDoubleHeight = MathF.PI / (2f * height);


        var u = 0;
        var v = 0;

        // calculate first coefficient in first row then use constant twice
        DoDctCoefficient(input: input,
            height: height,
            width: width,
            u: u,
            v: v,
            piDividedByDoubleWidth: piDividedByDoubleWidth,
            piDividedByDoubleHeight: piDividedByDoubleHeight,
            coeffs: coeffs,
            multiplier: alphaIfUIsZero * alphaIfUIsZero * beta);

        // calculate other coefficients in first row
        for (v = 1; v < width; v++)
        {
            DoDctCoefficient(input: input,
                height: height,
                width: width,
                u: u,
                v: v,
                piDividedByDoubleWidth: piDividedByDoubleWidth,
                piDividedByDoubleHeight: piDividedByDoubleHeight,
                coeffs: coeffs,
                multiplier: alphaIfUIsZero * beta);
        }

        // calculate coefficients in other rows 
        for (u = 1; u < height; u++)
        {
            v = 0;

            // calculate first coefficient in row
            DoDctCoefficient(input: input,
                height: height,
                width: width,
                u: u,
                v: v,
                piDividedByDoubleWidth: piDividedByDoubleWidth,
                piDividedByDoubleHeight: piDividedByDoubleHeight,
                coeffs: coeffs,
                multiplier: alphaIfUIsZero * beta);

            for (v = 1; v < width; v++)
            {
                DoDctCoefficient(input: input,
                    height: height,
                    width: width,
                    u: u,
                    v: v,
                    piDividedByDoubleWidth: piDividedByDoubleWidth,
                    piDividedByDoubleHeight: piDividedByDoubleHeight,
                    coeffs: coeffs,
                    multiplier: beta);
            }
        }

        return coeffs;
    }

    private static void DoDctCoefficient(
        float[,] input,
        int height,
        int width,
        int u,
        int v,
        float piDividedByDoubleWidth,
        float piDividedByDoubleHeight,
        float[,] coeffs,
        float multiplier)
    {
        var sum = 0f;
        for (var y = 0; y < height; y++)
        {
            var doubleYPusOne = 2f * y + 1f;
            for (var x = 0; x < width; x++)
            {
                sum += BasisFunction(a: input[x, y],
                    u: u,
                    v: v,
                    doubleXPlusOne: 2f * x + 1f,
                    doubleYPusOne: doubleYPusOne,
                    piDividedByDoubleWidth: piDividedByDoubleWidth,
                    piDividedByDoubleHeight: piDividedByDoubleHeight);
            }
        }

        coeffs[u, v] = sum * multiplier;
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

                var doubleXPlusOne = 2f * x + 1f;
                var doubleYPusOne = 2f * y + 1f;
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
        float u,
        float v,
        float doubleXPlusOne,
        float doubleYPusOne,
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
}
