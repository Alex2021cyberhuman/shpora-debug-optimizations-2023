using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        var rectangle = new Rectangle(0,
            0,
            bmp.Width,
            bmp.Height);
        var bmpData = bmp.LockBits(rectangle,
            ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var start = (byte*)bmpData.Scan0;
        var bmpStride = bmpData.Stride;
        Parallel.For(0,
            height,
            (
                int row) =>
            {
                var rowPointer = start + bmpStride * row;
                var column = 0;
                while (column != width)
                {
                    matrix.Pixels[row, column] = new(
                        *(rowPointer + 2),
                        *(rowPointer + 1),
                        *rowPointer);
                    rowPointer += 3;
                    column++;
                }
            });

        bmp.UnlockBits(bmpData);

        return matrix;
    }

    public static unsafe explicit operator Bitmap(
        Matrix matrix)
    {
        var matrixWidth = matrix.Width;
        var matrixHeight = matrix.Height;
        var bmp = new Bitmap(matrixWidth,
            matrixHeight,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var width = matrixWidth;

        var rectangle = new Rectangle(0,
            0,
            bmp.Width,
            bmp.Height);
        var bmpData = bmp.LockBits(rectangle,
            ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var start = (byte*)bmpData.Scan0;
        var bmpStride = bmpData.Stride;
        Parallel.For(0,
            matrixHeight,
            (
                int row) =>
            {
                var rowPointer = start + bmpStride * row;
                var column = 0;
                while (column != width)
                {
                    var pixel = matrix.Pixels[row, column];
                    *rowPointer = ToByte(pixel.B);
                    *(rowPointer + 1) = ToByte(pixel.G);
                    *(rowPointer + 2) = ToByte(pixel.R);
                    rowPointer += 3;
                    column++;
                }
            });
        
        bmp.UnlockBits(bmpData);

        return bmp;

        [MethodImpl(MethodImplOptions.AggressiveInlining |
                    MethodImplOptions.AggressiveOptimization)]
        static byte ToByte(
            float d)
        {
            return (byte)float.Clamp(d,
                0f,
                255f);
        }
    }
}
