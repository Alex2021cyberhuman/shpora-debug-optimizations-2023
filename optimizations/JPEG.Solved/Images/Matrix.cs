using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;

namespace JPEG.Solved.Images;

class Matrix
{
    public readonly Pixel[,] Pixels;
    public readonly int Height;
    public readonly int Width;

    public Matrix(
        int height,
        int width)
    {
        Height = height;
        Width = width;

        Pixels = new Pixel[height, width];
    }

    public static unsafe explicit operator Matrix(
        Bitmap bmp)
    {
        if (bmp.PixelFormat !=
            System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            throw new InvalidOperationException(
                "Cannot handle PixelFormat except Format24bppRgb");
        var height = bmp.Height - bmp.Height % 8;
        var width = bmp.Width - bmp.Width % 8;
        var matrix = new Matrix(height, width);

        var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rectangle, ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var pointer = (byte*)bmpData.Scan0;
        var matrixEnding = matrix.Height * width;
        if (bmpData.Stride > 0)
        {
            var indexer = 0;
            while (indexer != matrixEnding)
            {
                SetMatrixPixel(indexer);
                indexer++;
                pointer += 3;
            }
        }
        else
        {
            var indexer = matrixEnding - 1;
            while (indexer != -1)
            {
                SetMatrixPixel(indexer);
                indexer--;
                pointer += 3;
            }
        }

        bmp.UnlockBits(bmpData);

        return matrix;

        [MethodImpl(MethodImplOptions.AggressiveInlining |
                    MethodImplOptions.AggressiveOptimization)]
        void SetMatrixPixel(
            int indexer) =>
            matrix.Pixels[indexer / width, indexer % width] =
                new(*(pointer + 2), *(pointer + 1), *pointer);
    }

    public static unsafe explicit operator Bitmap(
        Matrix matrix)
    {
        var bmp = new Bitmap(matrix.Width, matrix.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var width = matrix.Width;

        var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);
        var bmpData = bmp.LockBits(rectangle, ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var pointer = (byte*)bmpData.Scan0;
        var matrixEnding = matrix.Height * width;
        if (bmpData.Stride > 0)
        {
            var indexer = 0;
            while (indexer != matrixEnding)
            {
                SetBmpPixel(indexer);
                indexer++;
                pointer += 3;
            }
        }
        else
        {
            var indexer = matrixEnding - 1;
            while (indexer != -1)
            {
                SetBmpPixel(indexer);
                indexer--;
                pointer += 3;
            }
        }

        bmp.UnlockBits(bmpData);

        return bmp;

        [MethodImpl(MethodImplOptions.AggressiveInlining |
                    MethodImplOptions.AggressiveOptimization)]
        void SetBmpPixel(
            int indexer)
        {
            var pixel = matrix.Pixels[indexer / width, indexer % width];
            *pointer = ToByte(pixel.B);
            *(pointer + 1) = ToByte(pixel.G);
            *(pointer + 2) = ToByte(pixel.R);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining |
                    MethodImplOptions.AggressiveOptimization)]
        static byte ToByte(
            float d)
        {
            return (byte)float.Clamp(d, 0f, 255f);
        }
    }
}
