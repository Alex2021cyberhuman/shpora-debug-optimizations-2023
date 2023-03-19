using BenchmarkDotNet.Attributes;
using JPEG.Processor;
using JPEG.Solved.Processor;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 5)]
public class UncompressJpegProcessorBenchmark
{
    private IJpegProcessor _jpegProcessor = null!;
    private IJpegProcessor _boostedJpegProcessor = null!;
    private static readonly string imagePath = @"sample.bmp";

    private static readonly string compressedImagePath =
        imagePath + ".compressed." + JpegProcessor.CompressionQuality;

    private static readonly string uncompressedImagePath = imagePath +
        ".uncompressed." +
        JpegProcessor.CompressionQuality +
        ".bmp";

    [GlobalSetup]
    public void SetUp()
    {
        _jpegProcessor = JpegProcessor.Init;
        _boostedJpegProcessor = BoostedJpegProcessor.Init;
    }

    [Benchmark]
    public void Uncompress()
    {
        _jpegProcessor.Uncompress(compressedImagePath, uncompressedImagePath);
    }

    [Benchmark(Baseline = true)]
    public void UncompressBosted()
    {
        _boostedJpegProcessor.Uncompress(compressedImagePath,
            uncompressedImagePath);
    }
}
