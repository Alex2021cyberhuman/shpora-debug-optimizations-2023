using System;

namespace JPEG.Solved;

public partial class DCT
{
    public static void DCT2DSubsamplingForCb(
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
            coeffs[u, v] = GetDctCoefficientSampledBy2(input: input,
                height: height,
                width: width,
                u: u,
                v: v,
                piDividedByDoubleWidth: piDividedByDoubleWidth,
                piDividedByDoubleHeight: piDividedByDoubleHeight,
                multiplier: multiplier);
        }
    }

    private static float GetDctCoefficientSampledBy2(
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
        for (var y = 0; y < height; y ++)
        {
            var doubleYPusOne = 2 * y + 1;
            for (var x = 0; x < width; x += 2)
            {
                sum += BasisFunction(a: (input[x, y] + input[x + 1, y]) / 2f,
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
