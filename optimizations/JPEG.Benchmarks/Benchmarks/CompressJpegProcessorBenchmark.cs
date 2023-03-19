using BenchmarkDotNet.Attributes;
using JPEG.Processor;
using JPEG.Solved.Processor;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class CompressJpegProcessorBenchmark
{
    private IJpegProcessor _jpegProcessor = null!;
    private IJpegProcessor _boostedJpegProcessor = null!;
    private static readonly string imagePath = @"sample.bmp";

    private static readonly string compressedImagePath =
        imagePath + ".compressed." + JpegProcessor.CompressionQuality;

    [GlobalSetup]
    public void SetUp()
    {
        _jpegProcessor = JpegProcessor.Init;
        _boostedJpegProcessor = BoostedJpegProcessor.Init;
    }

    [Benchmark]
    public void Compress()
    {
        _jpegProcessor.Compress(imagePath, compressedImagePath);
    }

    [Benchmark(Baseline = true)]
    public void CompressBoosted()
    {
        _boostedJpegProcessor.Compress(imagePath, compressedImagePath);
    }
}
