﻿using System;
using System.Linq;

namespace JPEG.Solved.Images;

public readonly record struct Pixel
{
	public readonly PixelFormat format = PixelFormat.RGB;

	public readonly float r;
	public readonly float g;
	public readonly float b;

	public readonly float y;
	public readonly float cb;
	public readonly float cr;

	public Pixel(float firstComponent = 0f, float secondComponent = 0f, float thirdComponent = 0f, PixelFormat pixelFormat = PixelFormat.RGB)
	{
		format = pixelFormat;
		switch (pixelFormat)
		{
			case PixelFormat.RGB:
				r = firstComponent;
				g = secondComponent;
				b = thirdComponent;
				break;
			case PixelFormat.YCbCr:
				y = firstComponent;
				cb = secondComponent;
				cr = thirdComponent;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(pixelFormat), pixelFormat, null);
		}
	}

	public float R => format == PixelFormat.RGB ? r : (298.082f * y + 408.583f * Cr) / 256.0f - 222.921f;

	public float G =>
		format == PixelFormat.RGB ? g : (298.082f * Y - 100.291f * Cb - 208.120f * Cr) / 256.0f + 135.576f;

	public float B => format == PixelFormat.RGB ? b : (298.082f * Y + 516.412f * Cb) / 256.0f - 276.836f;

	public float Y => format == PixelFormat.YCbCr ? y : 16.0f + (65.738f * R + 129.057f * G + 24.064f * B) / 256.0f;
	
	public float Cb => format == PixelFormat.YCbCr ? cb : 128.0f + (-37.945f * R - 74.494f * G + 112.439f * B) / 256.0f;
	
	public float Cr => format == PixelFormat.YCbCr ? cr : 128.0f + (112.439f * R - 94.154f * G - 18.285f * B) / 256.0f;
}