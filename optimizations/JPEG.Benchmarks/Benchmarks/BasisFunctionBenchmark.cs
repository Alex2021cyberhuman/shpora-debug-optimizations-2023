using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace JPEG.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 1, iterationCount: 2)]
public class BasisFunctionBenchmark
{
    private const int OperationsPerInvoke = 10_000_000;
    float[] a;
    int[] u;
    int[] v;
    int[] doubleXPlusOne;
    int[] doubleYPlusOne;
    float piDividedByDoubleWidth;
    float piDividedByDoubleHeight;
    private Dictionary<int, float> hashCodeDictionary;

    private Dictionary<(float a, float doubleXPlusOne, float u, float
        doubleYPusOne, float v), float> tupleDictionary;

    private ConcurrentDictionary<int, float> hashCodeDictionaryConcurent;

    private ConcurrentDictionary<(float a, float doubleXPlusOne, float u, float
        doubleYPusOne, float v), float> tupleDictionaryConcurent;

    [GlobalSetup]
    public void Setup()
    {
        a = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => (float)Random.Shared.Next(-127, 127)).ToArray();
        u = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => Random.Shared.Next(0, 8)).ToArray();
        v = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => Random.Shared.Next(0, 8)).ToArray();
        doubleXPlusOne = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => Random.Shared.Next(0, 8) * 2 + 1).ToArray();
        doubleYPlusOne = Enumerable.Range(0, OperationsPerInvoke)
            .Select(_ => Random.Shared.Next(0, 8) * 2 + 1).ToArray();
        piDividedByDoubleWidth = MathF.PI / (2f * 8);
        piDividedByDoubleHeight = MathF.PI / (2f * 8);
        hashCodeDictionaryConcurent = new();
        tupleDictionaryConcurent = new();
        tupleDictionary = new();
        hashCodeDictionary = new();
    }

    // [GlobalCleanup]
    // public void CheckCollisions()
    // {
    //     // if (hashCodeDictionary.Count != tupleDictionary.Count)
    //         // throw new ApplicationException("Collisisons with hash code");
    // }
    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public void BasisFunctionCachedTupleSingleThread()
    {
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            var key = (a: a[i], doubleXPlusOne: doubleXPlusOne[i], u: u[i],
                doubleYPlusOne: doubleYPlusOne[i], v: v[i]);
            float result;
            if (!tupleDictionary.TryGetValue(key, out result))
                tupleDictionary[key] = result = key.a *
                                                MathF.Cos(
                                                    x: key.doubleXPlusOne *
                                                    key.u *
                                                    piDividedByDoubleWidth) *
                                                MathF.Cos(
                                                    x: key.doubleYPlusOne *
                                                    key.v *
                                                    piDividedByDoubleHeight);
            ;
        }
    }


    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public void BasisFunctionCachedHashCodeSingleThread()
    {
        // hash code can give us collisions, but we ignore it now for tests
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            var key = HashCode.Combine(a[i],
                doubleXPlusOne[i],
                u[i],
                doubleYPlusOne[i],
                v[i]);
            float result;
            if (!hashCodeDictionary.TryGetValue(key, out result))
                hashCodeDictionary[key] = result = a[i] *
                                                MathF.Cos(
                                                    x: doubleXPlusOne[i] *
                                                    u[i] *
                                                    piDividedByDoubleWidth) *
                                                MathF.Cos(
                                                    x: doubleYPlusOne[i] *
                                                    v[i] *
                                                    piDividedByDoubleHeight);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public void BasisFunctionCachedTuple()
    {
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            var result = tupleDictionaryConcurent.GetOrAdd(
                (a[i], doubleXPlusOne[i], u[i], doubleYPlusOne[i], v[i]),
                tuple => tuple.a *
                         MathF.Cos(x: tuple.doubleXPlusOne *
                                      tuple.u *
                                      piDividedByDoubleWidth) *
                         MathF.Cos(x: tuple.doubleYPusOne *
                                      tuple.v *
                                      piDividedByDoubleHeight));
        }
    }


    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public void BasisFunctionCachedHashCode()
    {
        // hash code can give us collisions, but we ignore it now for tests
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            var result = hashCodeDictionaryConcurent.GetOrAdd(
                HashCode.Combine(a[i],
                    doubleXPlusOne[i],
                    u[i],
                    doubleYPlusOne[i],
                    v[i]),
                _ => a[i] *
                     MathF.Cos(x: doubleXPlusOne[i] *
                                  u[i] *
                                  piDividedByDoubleWidth) *
                     MathF.Cos(x: doubleYPlusOne[i] *
                                  v[i] *
                                  piDividedByDoubleHeight));
        }
    }


    [Benchmark(Baseline = true, OperationsPerInvoke = OperationsPerInvoke)]
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining |
                                   MethodImplOptions.AggressiveOptimization)]
    public void BasisFunction()
    {
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            var result = a[i] *
                         MathF.Cos(x: doubleXPlusOne[i] *
                                      u[i] *
                                      piDividedByDoubleWidth) *
                         MathF.Cos(x: doubleYPlusOne[i] *
                                      v[i] *
                                      piDividedByDoubleHeight);
        }
    }
}
