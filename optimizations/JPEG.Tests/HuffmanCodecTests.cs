using JPEG.Solved;

namespace JPEG.Tests;

public class HuffmanCodecTests
{
    [Test]
    public void CalcFrequencesAsNewWay()
    {
        var data = new byte[1_000_000];
        Random.Shared.NextBytes(data);
        var expected = new int[byte.MaxValue + 1];
        Parallel.ForEach(data, b => Interlocked.Increment(ref expected[b]));

        var actual = HuffmanCodec.CalcFrequences(data);
        
        Assert.That(actual, Is.EquivalentTo(expected));
    }
}
