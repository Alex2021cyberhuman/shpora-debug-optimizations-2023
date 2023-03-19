using BenchmarkDotNet.Attributes;
using JPEG.Solved;

namespace JPEG.Benchmarks.Benchmarks;

[SimpleJob(1,
    2,
    5)]
public class CalcFrequencesBenchmark
{
    private byte[] data;
    private int[] result1;
    private int[] result2;
    private int[] result3;

    [IterationSetup]
    public void CalcFrequencesAsNewWay()
    {
        data = new byte[50_000_000];
        Random.Shared.NextBytes(data);
        result1 = new int[byte.MaxValue + 1];
        result2 = new int[byte.MaxValue + 1];
        result3 = new int[byte.MaxValue + 1];
    }

    [Benchmark(Baseline = true)]
    public void ParallelIncrement()
    {
        Parallel.ForEach(data, b => Interlocked.Increment(ref result1[b]));
    }


    [Benchmark]
    public void ParallelByProcessorPartialResult()
    {
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
                    Interlocked.Add(ref result3[resultIndex], partialResult[resultIndex]);
                }
            });
    }

    [Benchmark]
    public void ParallelByProcessorSearch()
    {
        const int byteMaxValue = byte.MaxValue + 1;
        var processorCount = Environment.ProcessorCount;
        var rowsPerProcessor = byteMaxValue / processorCount;
        Parallel.For(0,
            processorCount,
            body: (
                int processor) =>
            {
                var num = processor * rowsPerProcessor;
                var end = (processor + 1) * rowsPerProcessor;
                if (processor == processorCount - 1 &&
                    byteMaxValue % processorCount != 0)
                    end += byteMaxValue % processorCount;

                for (; num != end; num++)
                {
                    foreach (var t in data)
                    {
                        if (t == num)
                            result2[num]++;
                    }
                }
            });
    }
}
