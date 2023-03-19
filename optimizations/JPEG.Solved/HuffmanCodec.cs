using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JPEG.Solved.Utilities;

namespace JPEG.Solved;

public class HuffmanCodec
{
    public static byte[] Encode(
        byte[] data,
        out Dictionary<BitsWithLength, byte> decodeTable,
        out long bitsCount)
    {
        var frequences = CalcFrequences(data);

        var root = BuildHuffmanTree(frequences);

        var encodeTable = new BitsWithLength[byte.MaxValue + 1];
        FillEncodeTable(root, encodeTable);

        var bitsBuffer = new BitsBuffer();
        foreach (var b in data)
            bitsBuffer.Add(encodeTable[b]);

        decodeTable = CreateDecodeTable(encodeTable);

        return bitsBuffer.ToArray(out bitsCount);
    }

    public static byte[] Decode(
        byte[] encodedData,
        Dictionary<BitsWithLength, byte> decodeTable,
        long bitsCount)
    {
        var result = new List<byte>();

        byte decodedByte;
        var sample = new BitsWithLength { Bits = 0, BitsCount = 0 };
        for (var byteNum = 0; byteNum < encodedData.Length; byteNum++)
        {
            var b = encodedData[byteNum];
            for (var bitNum = 0;
                 bitNum < 8 && byteNum * 8 + bitNum < bitsCount;
                 bitNum++)
            {
                sample.Bits = (sample.Bits << 1) +
                              ((b & (1 << (8 - bitNum - 1))) != 0 ? 1 : 0);
                sample.BitsCount++;

                if (decodeTable.TryGetValue(sample, out decodedByte))
                {
                    result.Add(decodedByte);

                    sample.BitsCount = 0;
                    sample.Bits = 0;
                }
            }
        }

        return result.ToArray();
    }

    private static Dictionary<BitsWithLength, byte> CreateDecodeTable(
        BitsWithLength[] encodeTable)
    {
        var result =
            new Dictionary<BitsWithLength, byte>(new BitsWithLength.Comparer());
        for (var b = 0; b < encodeTable.Length; b++)
        {
            var bitsWithLength = encodeTable[b];
            if (bitsWithLength == null)
                continue;

            result[bitsWithLength] = (byte)b;
        }

        return result;
    }

    private static void FillEncodeTable(
        HuffmanNode node,
        BitsWithLength[] encodeSubstitutionTable,
        int bitvector = 0,
        int depth = 0)
    {
        if (node.LeafLabel != null)
            encodeSubstitutionTable[node.LeafLabel.Value] =
                new() { Bits = bitvector, BitsCount = depth };
        else
        {
            if (node.Left != null)
            {
                FillEncodeTable(node.Left,
                    encodeSubstitutionTable,
                    (bitvector << 1) + 1,
                    depth + 1);
                FillEncodeTable(node.Right,
                    encodeSubstitutionTable,
                    (bitvector << 1) + 0,
                    depth + 1);
            }
        }
    }

    private static HuffmanNode BuildHuffmanTree(
        int[] frequences)
    {
        var nodes = GetNodes(frequences);

        while (nodes.Count() > 1)
        {
            var firstMin = nodes.MinOrDefault(node => node.Frequency);
            nodes = nodes.Without(firstMin);
            var secondMin = nodes.MinOrDefault(node => node.Frequency);
            nodes = nodes.Without(secondMin);
            nodes = nodes.Concat(new HuffmanNode
            {
                Frequency = firstMin.Frequency + secondMin.Frequency,
                Left = secondMin,
                Right = firstMin
            }.ToEnumerable());
        }

        return nodes.First();
    }

    private static IEnumerable<HuffmanNode> GetNodes(
        int[] frequences)
    {
        return Enumerable.Range(0, byte.MaxValue + 1)
            .Select(num => new HuffmanNode
            {
                Frequency = frequences[num], LeafLabel = (byte)num
            }).Where(node => node.Frequency > 0).ToArray();
    }

    public static int[] CalcFrequences(
        byte[] data)
    {
        var result = new int[byte.MaxValue + 1];
        var length = data.Length;
        var processorCount = Environment.ProcessorCount;
        var perProcessor = length / processorCount;
        Parallel.For(0,
            processorCount,
            body: (
                int processor) =>
            {
                var index = processor * perProcessor;
                var end = (processor + 1) * perProcessor;
                if (processor == processorCount - 1 &&
                    length % processorCount != 0)
                    end += length % processorCount;
                var partialResult = new int[byte.MaxValue + 1];
                for (; index != end; index++)
                {
                    partialResult[data[index]]++;
                }

                for (var resultIndex = 0;
                     resultIndex < partialResult.Length;
                     resultIndex++)
                {
                    Interlocked.Add(ref result[resultIndex],
                        partialResult[resultIndex]);
                }
            });
        return result;
    }
}
