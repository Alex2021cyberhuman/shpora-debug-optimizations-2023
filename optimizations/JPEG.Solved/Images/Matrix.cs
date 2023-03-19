using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using JPEG.Solved.Processor;

namespace JPEG.Solved.Images;

class Matrix
{
    public readonly PixelYCbCr[,] Pixels;
    public readonly int Height;
    public readonly int Width;

    public Matrix(
        int height,
        int width)
    {
        Height = height;
        Width = width;

        Pixels = new PixelYCbCr[height, width];
    }


    public static unsafe Matrix LoadBitmap(
        Bitmap bmp,
        int blockSize)
    {
        if (bmp.PixelFormat !=
            System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            throw new InvalidOperationException(
                message: "Cannot handle PixelFormat except Format24bppRgb");
        var height = bmp.Height - bmp.Height % blockSize;
        var width = bmp.Width - bmp.Width % blockSize;
        var matrix = new Matrix(height: height, width: width);

        var rectangle = new Rectangle(x: 0,
            y: 0,
            width: bmp.Width,
            height: bmp.Height);
        var bmpData = bmp.LockBits(rect: rectangle,
            flags: ImageLockMode.ReadOnly,
            format: bmp.PixelFormat);
        var start = (byte*)bmpData.Scan0;
        var bmpStride = bmpData.Stride;
        var processorCount = ProgramConstants.ProcessorCount;
        var rowsPerProcessor = height / processorCount;
        Parallel.For(fromInclusive: 0,
            toExclusive: processorCount,
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
                        matrix.Pixels[row, column] = new(r: *(rowPointer + 2),
                            g: *(rowPointer + 1),
                            b: *rowPointer);
                        rowPointer += 3;
                        column++;
                    }
                }
            });

        bmp.UnlockBits(bitmapdata: bmpData);

        return matrix;
    }

    public static explicit operator Matrix(
        Bitmap bmp)
    {
        return LoadBitmap(bmp: bmp, blockSize: 8);
    }

    public static unsafe explicit operator Bitmap(
        Matrix matrix)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var bmp = new Bitmap(width: width,
            height: height,
            format: System.Drawing.Imaging.PixelFormat.Format24bppRgb);
        var rectangle = new Rectangle(x: 0,
            y: 0,
            width: bmp.Width,
            height: bmp.Height);
        var bmpData = bmp.LockBits(rect: rectangle,
            flags: ImageLockMode.ReadOnly,
            format: bmp.PixelFormat);
        var start = (byte*)bmpData.Scan0;
        var bmpStride = bmpData.Stride;
        var processorCount = ProgramConstants.ProcessorCount;
        var rowsPerProcessor = height / processorCount;
        Parallel.For(fromInclusive: 0,
            toExclusive: processorCount,
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
                        matrix.Pixels[row, column].ToRgbBytesUnsafe(
                            b: rowPointer++,
                            g: rowPointer++,
                            r: rowPointer++);
                        column++;
                    }
                }
            });

        bmp.UnlockBits(bitmapdata: bmpData);

        return bmp;
    }
}
