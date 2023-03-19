using System;
using System.Drawing;
using System.Drawing.Imaging;
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

    
    public static unsafe Matrix LoadBitmap(
        Bitmap bmp,
        int blockSize)
    {
        if (bmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            throw new InvalidOperationException(
                "Cannot handle PixelFormat except Format24bppRgb");
        var height = bmp.Height - bmp.Height % blockSize;
        var width = bmp.Width - bmp.Width % blockSize;
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
        var processorCount = Environment.ProcessorCount;
        var rowsPerProcessor = height / processorCount;
        Parallel.For(0,
            processorCount,
            body: processor =>
            {
                var row = processor * rowsPerProcessor;
                var endingOfPart = (processor + 1) * rowsPerProcessor;
                if (processor == processorCount - 1 && height % processorCount != 0)
                    endingOfPart += height % processorCount;

                for (; row != endingOfPart; row++)
                {
                    var rowPointer = start + bmpStride * row;
                    var column = 0;
                    while (column != width)
                    {
                        matrix.Pixels[row, column] = new(*(rowPointer + 2),
                            *(rowPointer + 1),
                            *rowPointer);
                        rowPointer += 3;
                        column++;
                    }
                }
            });

        bmp.UnlockBits(bmpData);

        return matrix;
    }
    
    public static unsafe explicit operator Matrix(
        Bitmap bmp)
    {
        return LoadBitmap(bmp, 8);
    }

    public static unsafe explicit operator Bitmap(
        Matrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var bmp = new Bitmap(width,
            height,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var rectangle = new Rectangle(0,
            0,
            bmp.Width,
            bmp.Height);
        var bmpData = bmp.LockBits(rectangle,
            ImageLockMode.ReadOnly,
            bmp.PixelFormat);
        var start = (byte*)bmpData.Scan0;
        var bmpStride = bmpData.Stride;
        var processorCount = Environment.ProcessorCount;
        var rowsPerProcessor = height / processorCount;
        Parallel.For(0,
            processorCount,
            body: processor =>
            {
                var row = processor * rowsPerProcessor;
                var endingOfPart = (processor + 1) * rowsPerProcessor;
                if (processor == processorCount - 1 &&
                    height % processorCount != 0)
                    endingOfPart += height % processorCount;

                for (; row != endingOfPart; row++)
                {
                    var rowPointer = start + bmpStride * row;
                    var column = 0;
                    while (column != width)
                    {
                        var pixel = matrix.Pixels[row, column];
                        *rowPointer = pixel.B;
                        *(rowPointer + 1) = pixel.G;
                        *(rowPointer + 2) = pixel.R;
                        rowPointer += 3;
                        column++;
                    }
                }
            });

        bmp.UnlockBits(bmpData);

        return bmp;
    }
}
