namespace JPEG.Solved;

public partial class DCT
{
    public static void IDCT2DSubsamplingBy2(
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

                sum += BasisFunction(a: (coeffs[u, v] + coeffs[u, v] + 2) / 2f,
                           u: u,
                           v: v,
                           doubleXPlusOne: doubleXPlusOne,
                           doubleYPlusOne: doubleYPusOne) *
                       2f *
                       alphaIfUIsZero *
                       alphaIfUIsZero;

                for (u = 1; u < BlockSize; u++)
                {
                    sum += BasisFunction(
                               a: (coeffs[u, v] + coeffs[u, v + 1]) / 2,
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPlusOne: doubleYPusOne) *
                           2 *
                           alphaIfUIsZero;
                }

                for (v = 2; v < BlockSize; v += 2)
                {
                    u = 0;
                    sum += BasisFunction(
                               a: (coeffs[u, v] + coeffs[u, v + 1]) / 2,
                               u: u,
                               v: v,
                               doubleXPlusOne: doubleXPlusOne,
                               doubleYPlusOne: doubleYPusOne) *
                           2 *
                           alphaIfUIsZero;
                    for (u = 1; u < BlockSize; u++)
                    {
                        sum += BasisFunction(
                                   a: (coeffs[u, v] + coeffs[u, v + 1]) / 2,
                                   u: u,
                                   v: v,
                                   doubleXPlusOne: doubleXPlusOne,
                                   doubleYPlusOne: doubleYPusOne) *
                               2;
                    }
                }

                output[x, y] = sum * beta;
            }
        }
    }


    public static void DCT2DSubsamplingBy2(
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
            coeffs[u, v] = GetDctCoefficientSampledBy2(input: input,
                u: u,
                v: v,
                multiplier: multiplier);
        }
    }

    private static float GetDctCoefficientSampledBy2(
        float[,] input,
        int u,
        int v,
        float multiplier)
    {
        var sum = 0f;
        for (var x = 0; x < BlockSize; x += 2)
        {
            var doubleXPlusOne = 2 * x + 1;
            for (var y = 0; y < BlockSize; y++)
            {
                sum += BasisFunction(a: (input[x, y] + input[x + 1, y]) / 2,
                           u: u,
                           v: v,
                           doubleXPlusOne: doubleXPlusOne,
                           doubleYPlusOne: 2 * y + 1) *
                       2;
            }
        }

        return sum * multiplier;
    }
}
