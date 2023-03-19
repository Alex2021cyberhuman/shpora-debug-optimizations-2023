using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using JPEG.Processor;
using JPEG.Solved.Images;

namespace JPEG.Solved.Processor;

public class BoostedJpegProcessor : IJpegProcessor
{
    public static readonly BoostedJpegProcessor Init = new();

    public const int CompressionQuality = 70;
    public const int BlockSize = 8;
    private const int PartLength = BlockSize * BlockSize;

    public void Compress(
        string imagePath,
        string compressedImagePath)
    {
        using var fileStream = File.OpenRead(imagePath);
        using var bmp = Image.FromFile(imagePath, false) as Bitmap;
        var imageMatrix = (Matrix)bmp;
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
        var processorCount = ProgramConstants.ProcessorCount;
        var rowsPerProcessor = rowsOfBlocks / processorCount;
        Parallel.For(0,
            processorCount,
            body: processor =>
            {
                var channelFrequencies = new float[BlockSize, BlockSize];
                var subMatrixY = new float[BlockSize, BlockSize];
                var subMatrixCb = new float[BlockSize, BlockSize];
                var subMatrixCr = new float[BlockSize, BlockSize];
                var quantizedFreqs = new byte[BlockSize, BlockSize];
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

                    var x = 0;
                    for (; x < matrixWidth; x += BlockSize)
                    {
                        GetSubMatrix(matrix,
                            y,
                            x,
                            subMatrixY,
                            subMatrixCb,
                            subMatrixCr);

                        DCT.DCT2D(subMatrixY, channelFrequencies);
                        Quantize(channelFrequencies,
                            quantizationMatrix,
                            quantizedFreqs);
                        ZigZagScanAndWrite(quantizedFreqs,
                            output,
                            offset,
                            out offset);

                        DCT.DCT2D(subMatrixCb,
                            channelFrequencies);
                        Quantize(channelFrequencies,
                            quantizationMatrix,
                            quantizedFreqs);
                        ZigZagScanAndWrite(quantizedFreqs,
                            output,
                            offset,
                            out offset);

                        DCT.DCT2D(subMatrixCr,
                            channelFrequencies);
                        Quantize(channelFrequencies,
                            quantizationMatrix,
                            quantizedFreqs);
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
        var result = new Matrix(height: image.Height, width: image.Width);
        var decode = HuffmanCodec.Decode(encodedData: image.CompressedBytes,
            decodeTable: image.DecodeTable,
            bitsCount: image.BitsCount);
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        var lineBlockSize = image.Width * 3 * BlockSize;
        var rowsOfBlocks = imageHeight / BlockSize;
        var quantizationMatrix = GetQuantizationMatrix(image.Quality);
        Parallel.For(fromInclusive: 0,
            toExclusive: rowsOfBlocks,
            body: yIndex =>
            {
                var y = yIndex * BlockSize;
                var pointer = lineBlockSize * yIndex;
                var yChannel = new float[BlockSize, BlockSize];
                var cbChannel = new float[BlockSize, BlockSize];
                var crChannel = new float[BlockSize, BlockSize];
                var channelFreqs = new float[BlockSize, BlockSize];
                var quantizedFreqs = new byte[BlockSize, BlockSize];
                for (var x = 0; x < imageWidth; x += BlockSize)
                {
                    var quantizedBytes = decode.AsSpan(pointer, PartLength);
                    pointer += PartLength;
                    ZigZagUnScan(quantizedBytes, quantizedFreqs);
                    DeQuantize(quantizedFreqs,
                        channelFreqs,
                        quantizationMatrix);
                    DCT.IDCT2D(channelFreqs, yChannel);
                    ShiftMatrixValues(yChannel, 128);

                    quantizedBytes = decode.AsSpan(pointer, PartLength);
                    pointer += PartLength;
                    ZigZagUnScan(quantizedBytes, quantizedFreqs);
                    DeQuantize(quantizedFreqs,
                        channelFreqs,
                        quantizationMatrix);
                    DCT.IDCT2DSubsamplingBy2(channelFreqs, cbChannel);
                    ShiftMatrixValues(cbChannel, 128);

                    quantizedBytes = decode.AsSpan(pointer, PartLength);
                    pointer += PartLength;
                    ZigZagUnScan(quantizedBytes, quantizedFreqs);
                    DeQuantize(quantizedFreqs,
                        channelFreqs,
                        quantizationMatrix);
                    DCT.IDCT2DSubsamplingBy2(channelFreqs, crChannel);
                    ShiftMatrixValues(crChannel, 128);

                    SetPixels(matrix: result,
                        a: yChannel,
                        b: cbChannel,
                        c: crChannel,
                        yOffset: y,
                        xOffset: x);
                }
            });

        return result;
    }

    private static void ShiftMatrixValues(
        float[,] block,
        int shiftValue)
    {
        for (var y = 0; y < BlockSize; y++)
        for (var x = 0; x < BlockSize; x++)
            block[y, x] += shiftValue;
    }

    private static void SetPixels(
        Matrix matrix,
        float[,] a,
        float[,] b,
        float[,] c,
        int yOffset,
        int xOffset)
    {
        for (var y = 0; y < BlockSize; y++)
        for (var x = 0; x < BlockSize; x++)
            matrix.Pixels[yOffset + y, xOffset + x] = new(a[y, x],
                b[y, x],
                c[y, x]);
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

    private static void ZigZagUnScan(
        ReadOnlySpan<byte> quantizedBytes,
        byte[,] output)
    {
        output[0, 0] = quantizedBytes[0];
        output[0, 1] = quantizedBytes[1];
        output[0, 2] = quantizedBytes[5];
        output[0, 3] = quantizedBytes[6];
        output[0, 4] = quantizedBytes[14];
        output[0, 5] = quantizedBytes[15];
        output[0, 6] = quantizedBytes[27];
        output[0, 7] = quantizedBytes[28];
        output[1, 0] = quantizedBytes[2];
        output[1, 1] = quantizedBytes[4];
        output[1, 2] = quantizedBytes[7];
        output[1, 3] = quantizedBytes[13];
        output[1, 4] = quantizedBytes[16];
        output[1, 5] = quantizedBytes[26];
        output[1, 6] = quantizedBytes[29];
        output[1, 7] = quantizedBytes[42];
        output[2, 0] = quantizedBytes[3];
        output[2, 1] = quantizedBytes[8];
        output[2, 2] = quantizedBytes[12];
        output[2, 3] = quantizedBytes[17];
        output[2, 4] = quantizedBytes[25];
        output[2, 5] = quantizedBytes[30];
        output[2, 6] = quantizedBytes[41];
        output[2, 7] = quantizedBytes[43];
        output[3, 0] = quantizedBytes[9];
        output[3, 1] = quantizedBytes[11];
        output[3, 2] = quantizedBytes[18];
        output[3, 3] = quantizedBytes[24];
        output[3, 4] = quantizedBytes[31];
        output[3, 5] = quantizedBytes[40];
        output[3, 6] = quantizedBytes[44];
        output[3, 7] = quantizedBytes[53];
        output[4, 0] = quantizedBytes[10];
        output[4, 1] = quantizedBytes[19];
        output[4, 2] = quantizedBytes[23];
        output[4, 3] = quantizedBytes[32];
        output[4, 4] = quantizedBytes[39];
        output[4, 5] = quantizedBytes[45];
        output[4, 6] = quantizedBytes[52];
        output[4, 7] = quantizedBytes[54];
        output[5, 0] = quantizedBytes[20];
        output[5, 1] = quantizedBytes[22];
        output[5, 2] = quantizedBytes[33];
        output[5, 3] = quantizedBytes[38];
        output[5, 4] = quantizedBytes[46];
        output[5, 5] = quantizedBytes[51];
        output[5, 6] = quantizedBytes[55];
        output[5, 7] = quantizedBytes[60];
        output[6, 0] = quantizedBytes[21];
        output[6, 1] = quantizedBytes[34];
        output[6, 2] = quantizedBytes[37];
        output[6, 3] = quantizedBytes[47];
        output[6, 4] = quantizedBytes[50];
        output[6, 5] = quantizedBytes[56];
        output[6, 6] = quantizedBytes[59];
        output[6, 7] = quantizedBytes[61];
        output[7, 0] = quantizedBytes[35];
        output[7, 1] = quantizedBytes[36];
        output[7, 2] = quantizedBytes[48];
        output[7, 3] = quantizedBytes[49];
        output[7, 4] = quantizedBytes[57];
        output[7, 5] = quantizedBytes[58];
        output[7, 6] = quantizedBytes[62];
        output[7, 7] = quantizedBytes[63];
    }

    private static void Quantize(
        float[,] channelFreqs,
        int[,] quantizationMatrix,
        byte[,] result)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            for (var x = 0; x < BlockSize; x++)
            {
                result[y, x] =
                    (byte)(channelFreqs[y, x] / quantizationMatrix[y, x]);
            }
        }
    }

    private static void DeQuantize(
        byte[,] quantizedBytes,
        float[,] result,
        int[,] quantizationMatrix)
    {
        for (var y = 0; y < BlockSize; y++)
        {
            for (var x = 0; x < BlockSize; x++)
            {
                result[y, x] = (sbyte)quantizedBytes[y, x] *
                               quantizationMatrix[y,
                                   x]; //NOTE cast to sbyte not to loose negative numbers
            }
        }
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
