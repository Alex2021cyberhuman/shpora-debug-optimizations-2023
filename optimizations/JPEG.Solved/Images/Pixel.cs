namespace JPEG.Solved.Images;

public readonly record struct PixelYCbCr
{
    private const float maxByteValue = byte.MaxValue + 1;
    public readonly float Y;
    public readonly float Cb;
    public readonly float Cr;

    public PixelYCbCr(
        byte r,
        byte g,
        byte b)
    {
        Y = 16.0f + (65.738f * r + 129.057f * g + 24.064f * b) / 256.0f;
        Cb = 128.0f + (-37.945f * r - 74.494f * g + 112.439f * b) / 256.0f;
        Cr = 128.0f + (112.439f * r - 94.154f * g - 18.285f * b) / 256.0f;
    }

    public PixelYCbCr(
        float y = 0f,
        float cb = 0f,
        float cr = 0f)
    {
        Y = y;
        Cb = cb;
        Cr = cr;
    }

    public unsafe void ToRgbBytesUnsafe(
        byte* b,
        byte* g,
        byte* r)
    {
        *r = (byte)float.Clamp(
            (298.082f * Y + 408.583f * Cr) / 256.0f - 222.921f,
            0f,
            maxByteValue);
        *g = (byte)float.Clamp(
            (298.082f * Y - 100.291f * Cb - 208.120f * Cr) / 256.0f + 135.576f,
            0f,
            maxByteValue);
        *b = (byte)float.Clamp(
            (298.082f * Y + 516.412f * Cb) / 256.0f - 276.836f,
            0f,
            maxByteValue);
    }

    // public byte R => format == PixelFormat.RGB
    //     ? r
    //     : (byte)float.Clamp((298.082f * y + 408.583f * cr) / 256.0f - 222.921f,
    //         0f,
    //         MaxValue);
    //
    // public byte G =>
    //     format == PixelFormat.RGB
    //         ? g
    //         : (byte)float.Clamp(
    //             (298.082f * y - 100.291f * cb - 208.120f * cr) / 256.0f +
    //             135.576f,
    //             0f,
    //             MaxValue);
    //
    // public byte B => format == PixelFormat.RGB
    //     ? b
    //     : (byte)float.Clamp((298.082f * y + 516.412f * cb) / 256.0f - 276.836f,
    //         0f,
    //         MaxValue);
    //
    // public float Y => format == PixelFormat.YCbCr
    //     ? y
    //     : 16.0f + (65.738f * R + 129.057f * G + 24.064f * B) / 256.0f;
    //
    // public float Cb => format == PixelFormat.YCbCr
    //     ? cb
    //     : 128.0f + (-37.945f * R - 74.494f * G + 112.439f * B) / 256.0f;
    //
    // public float Cr => format == PixelFormat.YCbCr
    //     ? cr
    //     : 128.0f + (112.439f * R - 94.154f * G - 18.285f * B) / 256.0f;
}