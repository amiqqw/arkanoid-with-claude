using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;

public struct BenchmarkResult
{
    public int N;
    public double AvgMs;
}

public static class Benchmark
{
    private static readonly (int rows, int cols)[] LevelSizes =
    {
        (5,   8),     // 40
        (10,  16),    // 160
        (20,  32),    // 640
        (40,  64),    // 2560
        (80,  128),   // 10240
        (160, 256),   // 40960
    };

    private static readonly int[] NearestSearchSizes =
    {
        100, 1000, 10000, 100000, 1000000
    };

    public static List<BenchmarkResult> RunLevelGeneration(int iterations = 50)
    {
        var results = new List<BenchmarkResult>();
        var sw = new Stopwatch();

        foreach (var (rows, cols) in LevelSizes)
        {
            sw.Restart();
            for (int i = 0; i < iterations; i++)
                Levels.GenerateRandom(rows, cols);
            sw.Stop();

            results.Add(new BenchmarkResult
            {
                N = rows * cols,
                AvgMs = sw.Elapsed.TotalMilliseconds / iterations
            });
        }
        return results;
    }

    public static List<BenchmarkResult> RunNearestSearch(int iterations = 10)
    {
        var results = new List<BenchmarkResult>();
        var rng = new Random(42);
        var sw = new Stopwatch();

        foreach (int n in NearestSearchSizes)
        {
            var positions = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                positions[i] = new Vector2(
                    (float)rng.NextDouble() * 480f,
                    (float)rng.NextDouble() * 640f
                );
            }
            Vector2 ballPos = new Vector2(240, 320);

            sw.Restart();
            for (int it = 0; it < iterations; it++)
                FindNearestNaive(ballPos, positions);
            sw.Stop();

            results.Add(new BenchmarkResult
            {
                N = n,
                AvgMs = sw.Elapsed.TotalMilliseconds / iterations
            });
        }
        return results;
    }

    private static int FindNearestNaive(Vector2 target, Vector2[] points)
    {
        int bestIdx = -1;
        float bestDistSq = float.MaxValue;
        for (int i = 0; i < points.Length; i++)
        {
            float dx = points[i].X - target.X;
            float dy = points[i].Y - target.Y;
            float d = dx * dx + dy * dy;
            if (d < bestDistSq)
            {
                bestDistSq = d;
                bestIdx = i;
            }
        }
        return bestIdx;
    }
}