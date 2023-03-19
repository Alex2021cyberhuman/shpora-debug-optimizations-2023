using System;

namespace JPEG.Solved.Fft;

public static class FFT
{
    private const int BlockSize = 8;

    private static readonly SingleComplex[,] complexes;

    static FFT()
    {
        complexes = new SingleComplex[BlockSize, BlockSize];
        for (var i = 0; i < BlockSize; i++)
        {
            var mult1 = -MathF.PI / i;
            for (var k = 0; k < i; k++)
            {
                complexes[i, k] = new(MathF.Cos(mult1 * k),
                    MathF.Sin(mult1 * k));
            }
        }
    }

    public static void InverseFft2d(
        float[,] coefficients,
        float[,] output,
        SingleComplex[,] calculation,
        int height,
        int width,
        SingleComplex[] columnsFftCache,
        SingleComplex[] rowsFftCache)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                calculation[x, y] = new(coefficients[x, y], 0);
            }
        }

        // Transform the columns
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                rowsFftCache[j] = calculation[i, j];
            }

            // Calling 1D FFT Function for Columns
            ConstrainedInvertFft(rowsFftCache);

            for (var j = 0; j < height; j++)
            {
                calculation[i, j] = rowsFftCache[i] with { Imaginary = 0f };
            }
        }

        // Transform the Rows
        for (var j = 0; j < height; j++)
        {
            for (var i = 0; i < width; i++)
            {
                columnsFftCache[i] = calculation[i, j];
            }

            // Calling 1D FFT Function for Rows
            ConstrainedInvertFft(columnsFftCache.AsSpan());

            for (var i = 0; i < width; i++)
            {
                output[i, j] = columnsFftCache[i].Real;
            }
        }
    }

    public static void ForwardFft2d(
        float[,] channels,
        float[,] output,
        SingleComplex[,] calculation,
        int height,
        int width,
        SingleComplex[] columnsFftCache,
        SingleComplex[] rowsFftCache)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                calculation[x, y] = new(channels[x, y], 0f);
            }
        }

        // Transform the Rows
        for (var j = 0; j < height; j++)
        {
            for (var i = 0; i < width; i++)
            {
                columnsFftCache[i] = calculation[i, j];
            }

            // Calling 1D FFT Function for Rows
            ConstrainedFft(columnsFftCache.AsSpan());

            for (var i = 0; i < width; i++)
            {
                calculation[i, j] = columnsFftCache[i] with { Imaginary = 0f };
            }
        }

        // Transform the columns
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                rowsFftCache[i] = calculation[i, j];
            }

            // Calling 1D FFT Function for Columns
            ConstrainedFft(rowsFftCache.AsSpan());

            for (var j = 0; j < height; j++)
            {
                output[i, j] = rowsFftCache[j].Real;
            }
        }
    }

    /// <summary>
    /// High performance FFT function.
    /// Complex input will be transformed in place.
    /// Input array length must be a power of two. This length is NOT validated.
    /// Running on an array with an invalid length may throw an invalid index exception.
    /// </summary>
    private static void ConstrainedFft(
        Span<SingleComplex> buffer)
    {
        for (var i = 1; i < buffer.Length; i++)
        {
            var j = BitReverse(i, buffer.Length);
            if (j > i) (buffer[j], buffer[i]) = (buffer[i], buffer[j]);
        }

        for (var i = 1; i <= buffer.Length / 2; i *= 2)
        {
            for (var j = 0; j < buffer.Length; j += i * 2)
            {
                for (var k = 0; k < i; k++)
                {
                    var evenI = j + k;
                    var oddI = j + k + i;
                    var temp = complexes[i, k];
                    temp *= buffer[oddI];
                    buffer[oddI] = buffer[evenI] - temp;
                    buffer[evenI] += temp;
                }
            }
        }
    }

    private static void ConstrainedInvertFft(
        Span<SingleComplex> buffer)
    {
        // invert the imaginary component
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = new(buffer[i].Real, -buffer[i].Imaginary);

        // perform a forward Fourier transform
        ConstrainedFft(buffer);

        // invert the imaginary component again and scale the output
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = new(buffer[i].Real / buffer.Length,
                -buffer[i].Imaginary / buffer.Length);
    }

    /// <summary>
    /// Reverse the sequence of bits in an integer (01101 -> 10110)
    /// </summary>
    private static int BitReverse(
        int value,
        int maxValue)
    {
        var maxBitCount = int.Log2(maxValue);
        var output = value;
        var bitCount = maxBitCount - 1;

        value >>= 1;
        while (value > 0)
        {
            output = (output << 1) | (value & 1);
            bitCount -= 1;
            value >>= 1;
        }

        return (output << bitCount) & ((1 << maxBitCount) - 1);
    }
}
