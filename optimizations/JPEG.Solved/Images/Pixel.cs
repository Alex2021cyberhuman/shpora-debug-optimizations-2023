using System;

namespace JPEG.Solved.Images;

public readonly record struct Pixel
{
    private const float MaxValue = byte.MaxValue;
    public readonly PixelFormat format = PixelFormat.RGB;

    private readonly byte r;
    private readonly byte g;
    private readonly byte b;

    private readonly float y;
    private readonly float cb;
    private readonly float cr;

    public Pixel(
        byte firstComponent,
        byte secondComponent,
        byte thirdComponent)
    {
        r = firstComponent;
        g = secondComponent;
        b = thirdComponent;
        format = PixelFormat.RGB;
    }

    public Pixel(
        float firstComponent = 0f,
        float secondComponent = 0f,
        float thirdComponent = 0f,
        PixelFormat pixelFormat = PixelFormat.RGB)
    {
        format = pixelFormat;
        switch (pixelFormat)
        {
            case PixelFormat.RGB:
                r = (byte)firstComponent;
                g = (byte)secondComponent;
                b = (byte)thirdComponent;
                break;
            case PixelFormat.YCbCr:
                y = firstComponent;
                cb = secondComponent;
                cr = thirdComponent;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pixelFormat),
                    pixelFormat,
                    null);
        }
    }

    public byte R => format == PixelFormat.RGB
        ? r
        : (byte)float.Clamp((298.082f * y + 408.583f * cr) / 256.0f - 222.921f,
            0f,
            MaxValue);

    public byte G =>
        format == PixelFormat.RGB
            ? g
            : (byte)float.Clamp(
                (298.082f * y - 100.291f * cb - 208.120f * cr) / 256.0f +
                135.576f,
                0f,
                MaxValue);

    public byte B => format == PixelFormat.RGB
        ? b
        : (byte)float.Clamp((298.082f * y + 516.412f * cb) / 256.0f - 276.836f,
            0f,
            MaxValue);

    public float Y => format == PixelFormat.YCbCr
        ? y
        : 16.0f + (65.738f * R + 129.057f * G + 24.064f * B) / 256.0f;

    public float Cb => format == PixelFormat.YCbCr
        ? cb
        : 128.0f + (-37.945f * R - 74.494f * G + 112.439f * B) / 256.0f;

    public float Cr => format == PixelFormat.YCbCr
        ? cr
        : 128.0f + (112.439f * R - 94.154f * G - 18.285f * B) / 256.0f;
}
