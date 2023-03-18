using BenchmarkDotNet.Attributes;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class AccessOrMultiplyBenchmark
{
    private const int OperationsPerInvoke = 10000;

    private float[] results;
    private int n;

    [Params(100,
        1000,
        3000,
        500000)]
    public int DistinctValues { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        results = Enumerable.Range(0, DistinctValues).Select(x => 2f * x + 1f)
            .ToArray();
    }

    [IterationSetup]
    public void SetupNumber()
    {
        n = Random.Shared.Next(0, DistinctValues);
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void Access()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            var result = results[n];
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    public void Multiply()
    {
        for (int i = 0; i < OperationsPerInvoke; i++)
        {
            var result = 2f * n + 1f;
        }
    }
}
