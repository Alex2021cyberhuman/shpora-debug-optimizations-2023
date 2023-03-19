using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using JPEG.Processor;
using JPEG.Solved.Fft;
using JPEG.Solved.Images;
using PixelFormat = JPEG.Solved.Images.PixelFormat;

namespace JPEG.Solved.Processor;

public class BrokenBoostedJpegProcessor : IJpegProcessor
{
    public static readonly BrokenBoostedJpegProcessor Init = new();

    public const int CompressionQuality = 70;
    private const int BlockSize = 8;
    private const int PartLength = BlockSize * BlockSize;

    public void Compress(
        string imagePath,
        string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = (Bitmap)Image.FromStream(fileStream,
            false,
            false);
        var imageMatrix = Matrix.LoadBitmap(bmp, BlockSize);
        var compressionResult = Compress(imageMatrix, CompressionQuality);
        compressionResult.Save(compressedImagePath);
    }

    public void Uncompress(
        string compressedImagePath,
        string uncompressedImagePath)
    {
        var compressedImage = CompressedImage.Load(compressedImagePath);
        var uncompressedImage = Uncompress(compressedImage);
        var resultBmp = (Bitmap)uncompressedImage;
        resultBmp.Save(uncompressedImagePath, ImageFormat.Bmp);
    }

    private static CompressedImage Compress(
        Matrix matrix,
        int quality = 50)
    {
        const int selectorsLength = 3;
        var matrixWidth = matrix.Width;
        var matrixHeight = matrix.Height;
        var output = new byte[matrixHeight /
                              BlockSize *
                              matrixWidth *
                              BlockSize *
                              selectorsLength];
        var rowsOfBlocks = matrixHeight / BlockSize;
        var quantizationMatrix = GetQuantizationMatrix(quality);
        var processorCount = Environment.ProcessorCount;
        var rowsPerProcessor = rowsOfBlocks / processorCount;
        Parallel.For(0,
            processorCount,
            body: processor =>
            {
                var yIndex = processor * rowsPerProcessor;
                var endingOfPart = (processor + 1) * rowsPerProcessor;
                if (processor == processorCount - 1 &&
                    rowsOfBlocks % processorCount != 0)
                    endingOfPart += rowsOfBlocks % processorCount;

                for (; yIndex != endingOfPart; yIndex++)
                {
                    var offset = matrixWidth *
                                 BlockSize *
                                 selectorsLength *
                                 yIndex;
                    var y = yIndex * BlockSize;
                    var channelFrequencies = new float[BlockSize, BlockSize];
                    var subMatrixY = new float[BlockSize, BlockSize];
                    var subMatrixCb = new float[BlockSize, BlockSize];
                    var subMatrixCr = new float[BlockSize, BlockSize];
                    var calculation = new SingleComplex[BlockSize, BlockSize];
                    var columnsFftCache = new SingleComplex[BlockSize];
                    var rowsFftCache = new SingleComplex[BlockSize];
                    var x = 0;
                    for (; x < matrixWidth; x += BlockSize)
                    {
                        GetSubMatrix(matrix,
                            y,
                            x,
                            subMatrixY,
                            subMatrixCb,
                            subMatrixCr);

                        FFT.ForwardFft2d(subMatrixY,
                            channelFrequencies,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);

                        var quantizedFreqs = Quantize(channelFrequencies,
                            quantizationMatrix);
                        ZigZagScanAndWrite(quantizedFreqs,
                            output,
                            offset,
                            out offset);

                        FFT.ForwardFft2d(subMatrixCb,
                            channelFrequencies,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);

                        quantizedFreqs = Quantize(channelFrequencies,
                            quantizationMatrix);
                        ZigZagScanAndWrite(quantizedFreqs,
                            output,
                            offset,
                            out offset);

                        FFT.ForwardFft2d(subMatrixCr,
                            channelFrequencies,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);

                        quantizedFreqs = Quantize(channelFrequencies,
                            quantizationMatrix);
                        ZigZagScanAndWrite(quantizedFreqs,
                            output,
                            offset,
                            out offset);
                    }
                }
            });

        var compressedBytes = HuffmanCodec.Encode(output,
            out var decodeTable,
            out var bitsCount);

        return new()
        {
            Quality = quality,
            CompressedBytes = compressedBytes,
            BitsCount = bitsCount,
            DecodeTable = decodeTable,
            Height = matrixHeight,
            Width = matrixWidth
        };
    }

    private static Matrix Uncompress(
        CompressedImage image)
    {
        var result = new Matrix(image.Height, image.Width);
        var decode = HuffmanCodec.Decode(image.CompressedBytes,
            image.DecodeTable,
            image.BitsCount);
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        var lineBlockSize = image.Width * 3 * BlockSize;
        var rowsOfBlocks = imageHeight / BlockSize;
        var imageQuality = image.Quality;
        var processorCount = Environment.ProcessorCount;
        var rowsPerProcessor = rowsOfBlocks / processorCount;
        Parallel.For(0,
            processorCount,
            body: processor =>
            {
                var yIndex = processor * rowsPerProcessor;
                var endingOfPart = (processor + 1) * rowsPerProcessor;
                if (processor == processorCount - 1 &&
                    rowsOfBlocks % processorCount != 0)
                    endingOfPart += rowsOfBlocks % processorCount;

                for (; yIndex != endingOfPart; yIndex++)
                {
                    // var yIndex = y / DCTSize;
                    var y = yIndex * BlockSize;
                    var pixelsPointer = lineBlockSize * yIndex;
                    var yChannel = new float[BlockSize, BlockSize];
                    var cbChannel = new float[BlockSize, BlockSize];
                    var crChannel = new float[BlockSize, BlockSize];
                    var calculation = new SingleComplex[BlockSize, BlockSize];
                    var columnsFftCache = new SingleComplex[BlockSize];
                    var rowsFftCache = new SingleComplex[BlockSize];
                    for (var x = 0; x < imageWidth; x += BlockSize)
                    {
                        // y channel
                        var quantizedBytes =
                            decode.AsSpan(pixelsPointer, PartLength);
                        pixelsPointer += PartLength;
                        var quantizedFreqs = ZigZagUnScan(quantizedBytes);
                        var channelFreqs =
                            DeQuantize(quantizedFreqs, imageQuality);
                        FFT.InverseFft2d(channelFreqs,
                            yChannel,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);
                        ShiftMatrixValues(yChannel, 128);

                        // cb channel
                        quantizedBytes =
                            decode.AsSpan(pixelsPointer, PartLength);
                        pixelsPointer += PartLength;
                        quantizedFreqs = ZigZagUnScan(quantizedBytes);
                        channelFreqs = DeQuantize(quantizedFreqs, imageQuality);
                        FFT.InverseFft2d(channelFreqs,
                            cbChannel,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);
                        ShiftMatrixValues(cbChannel, 128);

                        // cr channel
                        quantizedBytes =
                            decode.AsSpan(pixelsPointer, PartLength);
                        pixelsPointer += PartLength;
                        quantizedFreqs = ZigZagUnScan(quantizedBytes);
                        channelFreqs = DeQuantize(quantizedFreqs, imageQuality);
                        FFT.InverseFft2d(channelFreqs,
                            crChannel,
                            calculation,
                            BlockSize,
                            BlockSize,
                            columnsFftCache,
                            rowsFftCache);
                        ShiftMatrixValues(crChannel, 128);

                        SetPixels(result,
                            yChannel,
                            cbChannel,
                            crChannel,
                            PixelFormat.YCbCr,
                            y,
                            x);
                    }
                }
            });

        return result;
    }

    private static void ShiftMatrixValues(
        float[,] subMatrix,
        int shiftValue)
    {
        var height = subMatrix.GetLength(0);
        var width = subMatrix.GetLength(1);
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            subMatrix[y, x] += shiftValue;
    }

    private static void SetPixels(
        Matrix matrix,
        float[,] a,
        float[,] b,
        float[,] c,
        PixelFormat format,
        int yOffset,
        int xOffset)
    {
        var height = a.GetLength(0);
        var width = a.GetLength(1);

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            matrix.Pixels[yOffset + y, xOffset + x] = new(a[y, x],
                b[y, x],
                c[y, x],
                format);
    }

    private static void GetSubMatrix(
        Matrix matrix,
        int yOffset,
        int xOffset,
        float[,] resultY,
        float[,] resultCb,
        float[,] resultCr)
    {
        for (var j = 0; j < BlockSize; j++)
        for (var i = 0; i < BlockSize; i++)
        {
            resultY[j, i] = matrix.Pixels[yOffset + j, xOffset + i].Y - 128f;
            resultCb[j, i] = matrix.Pixels[yOffset + j, xOffset + i].Cb - 128f;
            resultCr[j, i] = matrix.Pixels[yOffset + j, xOffset + i].Cr - 128f;
        }
    }

    private static void ZigZagScanAndWrite(
        byte[,] channelFreqs,
        byte[] output,
        int offset,
        out int endOffset)
    {
        output[offset++] = channelFreqs[0, 0];
        output[offset++] = channelFreqs[0, 1];
        output[offset++] = channelFreqs[1, 0];
        output[offset++] = channelFreqs[2, 0];
        output[offset++] = channelFreqs[1, 1];
        output[offset++] = channelFreqs[0, 2];
        output[offset++] = channelFreqs[0, 3];
        output[offset++] = channelFreqs[1, 2];
        output[offset++] = channelFreqs[2, 1];
        output[offset++] = channelFreqs[3, 0];
        output[offset++] = channelFreqs[4, 0];
        output[offset++] = channelFreqs[3, 1];
        output[offset++] = channelFreqs[2, 2];
        output[offset++] = channelFreqs[1, 3];
        output[offset++] = channelFreqs[0, 4];
        output[offset++] = channelFreqs[0, 5];
        output[offset++] = channelFreqs[1, 4];
        output[offset++] = channelFreqs[2, 3];
        output[offset++] = channelFreqs[3, 2];
        output[offset++] = channelFreqs[4, 1];
        output[offset++] = channelFreqs[5, 0];
        output[offset++] = channelFreqs[6, 0];
        output[offset++] = channelFreqs[5, 1];
        output[offset++] = channelFreqs[4, 2];
        output[offset++] = channelFreqs[3, 3];
        output[offset++] = channelFreqs[2, 4];
        output[offset++] = channelFreqs[1, 5];
        output[offset++] = channelFreqs[0, 6];
        output[offset++] = channelFreqs[0, 7];
        output[offset++] = channelFreqs[1, 6];
        output[offset++] = channelFreqs[2, 5];
        output[offset++] = channelFreqs[3, 4];
        output[offset++] = channelFreqs[4, 3];
        output[offset++] = channelFreqs[5, 2];
        output[offset++] = channelFreqs[6, 1];
        output[offset++] = channelFreqs[7, 0];
        output[offset++] = channelFreqs[7, 1];
        output[offset++] = channelFreqs[6, 2];
        output[offset++] = channelFreqs[5, 3];
        output[offset++] = channelFreqs[4, 4];
        output[offset++] = channelFreqs[3, 5];
        output[offset++] = channelFreqs[2, 6];
        output[offset++] = channelFreqs[1, 7];
        output[offset++] = channelFreqs[2, 7];
        output[offset++] = channelFreqs[3, 6];
        output[offset++] = channelFreqs[4, 5];
        output[offset++] = channelFreqs[5, 4];
        output[offset++] = channelFreqs[6, 3];
        output[offset++] = channelFreqs[7, 2];
        output[offset++] = channelFreqs[7, 3];
        output[offset++] = channelFreqs[6, 4];
        output[offset++] = channelFreqs[5, 5];
        output[offset++] = channelFreqs[4, 6];
        output[offset++] = channelFreqs[3, 7];
        output[offset++] = channelFreqs[4, 7];
        output[offset++] = channelFreqs[5, 6];
        output[offset++] = channelFreqs[6, 5];
        output[offset++] = channelFreqs[7, 4];
        output[offset++] = channelFreqs[7, 5];
        output[offset++] = channelFreqs[6, 6];
        output[offset++] = channelFreqs[5, 7];
        output[offset++] = channelFreqs[6, 7];
        output[offset++] = channelFreqs[7, 6];
        output[offset++] = channelFreqs[7, 7];
        endOffset = offset;
    }

    private static byte[,] ZigZagUnScan(
        ReadOnlySpan<byte> quantizedBytes)
    {
        return new[,]
        {
            {
                quantizedBytes[0], quantizedBytes[1], quantizedBytes[5],
                quantizedBytes[6], quantizedBytes[14], quantizedBytes[15],
                quantizedBytes[27], quantizedBytes[28]
            },
            {
                quantizedBytes[2], quantizedBytes[4], quantizedBytes[7],
                quantizedBytes[13], quantizedBytes[16], quantizedBytes[26],
                quantizedBytes[29], quantizedBytes[42]
            },
            {
                quantizedBytes[3], quantizedBytes[8], quantizedBytes[12],
                quantizedBytes[17], quantizedBytes[25], quantizedBytes[30],
                quantizedBytes[41], quantizedBytes[43]
            },
            {
                quantizedBytes[9], quantizedBytes[11], quantizedBytes[18],
                quantizedBytes[24], quantizedBytes[31], quantizedBytes[40],
                quantizedBytes[44], quantizedBytes[53]
            },
            {
                quantizedBytes[10], quantizedBytes[19], quantizedBytes[23],
                quantizedBytes[32], quantizedBytes[39], quantizedBytes[45],
                quantizedBytes[52], quantizedBytes[54]
            },
            {
                quantizedBytes[20], quantizedBytes[22], quantizedBytes[33],
                quantizedBytes[38], quantizedBytes[46], quantizedBytes[51],
                quantizedBytes[55], quantizedBytes[60]
            },
            {
                quantizedBytes[21], quantizedBytes[34], quantizedBytes[37],
                quantizedBytes[47], quantizedBytes[50], quantizedBytes[56],
                quantizedBytes[59], quantizedBytes[61]
            },
            {
                quantizedBytes[35], quantizedBytes[36], quantizedBytes[48],
                quantizedBytes[49], quantizedBytes[57], quantizedBytes[58],
                quantizedBytes[62], quantizedBytes[63]
            }
        };
    }

    private static byte[,] Quantize(
        float[,] channelFreqs,
        int[,] quantizationMatrix)
    {
        var height = channelFreqs.GetLength(0);
        var width = channelFreqs.GetLength(1);
        var result = new byte[height, width];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                result[y, x] =
                    (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
            }
        }

        return result;
    }

    private static float[,] DeQuantize(
        byte[,] quantizedBytes,
        int quality)
    {
        var quantizationMatrix = GetQuantizationMatrix(quality);

        var lengthBytesHeight = quantizedBytes.GetLength(0);
        var lengthBytesWidth = quantizedBytes.GetLength(1);
        var result = new float[lengthBytesHeight, lengthBytesWidth];
        for (var y = 0; y < lengthBytesHeight; y++)
        {
            for (var x = 0; x < lengthBytesWidth; x++)
            {
                result[y, x] = (sbyte)quantizedBytes[y, x] *
                               quantizationMatrix[y,
                                   x]; //NOTE cast to sbyte not to loose negative numbers
            }
        }

        return result;
    }

    private static int[,] GetQuantizationMatrix(
        int quality)
    {
        if (quality < 1 || quality > 99)
            throw new ArgumentException("quality must be in [1,99] interval");

        var multiplier = quality < 50 ? 5000 / quality : 200 - 2 * quality;

        var result = new[,]
        {
            { 16, 11, 10, 16, 24, 40, 51, 61 },
            { 12, 12, 14, 19, 26, 58, 60, 55 },
            { 14, 13, 16, 24, 40, 57, 69, 56 },
            { 14, 17, 22, 29, 51, 87, 80, 62 },
            { 18, 22, 37, 56, 68, 109, 103, 77 },
            { 24, 35, 55, 64, 81, 104, 113, 92 },
            { 49, 64, 78, 87, 103, 121, 120, 101 },
            { 72, 92, 95, 98, 112, 100, 103, 99 }
        };

        var resultHeight = result.GetLength(0);
        var resultWidth = result.GetLength(1);
        for (var y = 0; y < resultHeight; y++)
        {
            for (var x = 0; x < resultWidth; x++)
            {
                result[y, x] = (multiplier * result[y, x] + 50) / 100;
            }
        }

        return result;
    }
}
