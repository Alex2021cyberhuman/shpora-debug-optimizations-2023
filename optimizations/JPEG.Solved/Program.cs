using System;
using System.Diagnostics;
using System.Linq;
using JPEG.Solved.Processor;

Console.WriteLine(nint.Size == 8 ? "64-bit version" : "32-bit version");
var processor = BoostedJpegProcessor.Init;
var sw = Stopwatch.StartNew();
var imagePath = args.ElementAtOrDefault(0) ??  @"sample.bmp";
// var imageName = "Big_Black_River_Railroad_Bridge.bmp";
var compressedImagePath = imagePath +
                          ".compressed." +
                          BoostedJpegProcessor.CompressionQuality;
var uncompressedImagePath = imagePath +
                            ".uncompressed." +
                            BoostedJpegProcessor.CompressionQuality +
                            ".bmp";
for (int i = 0; i < 1; i++)
{
    sw.Restart();
    processor.Compress(imagePath, compressedImagePath);
    sw.Stop();
    Console.WriteLine("Compression: " + sw.ElapsedMilliseconds);
    
    sw.Restart();
    processor.Uncompress(compressedImagePath, uncompressedImagePath);
    sw.Stop();
    Console.WriteLine("Decompression: " + sw.ElapsedMilliseconds);
}
// Console.WriteLine(
//     $"Peak commit size: {MemoryMeter.PeakPrivateBytes() / (1024.0 * 1024):F2} MB");
// Console.WriteLine(
//     $"Peak working set: {MemoryMeter.PeakWorkingSet() / (1024.0 * 1024):F2} MB");
