// <copyright file="MatrixBenchmark.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ParallelMatrixMultiplication;

using System.Diagnostics;

/// <summary>
/// Provides benchmarking capabilities for matrix multiplication operations.
/// </summary>
public static class MatrixBenchmark
{
    /// <summary>
    /// Runs a benchmark comparing sequential and parallel matrix multiplication.
    /// </summary>
    /// <param name="matrixA">The first matrix to multiply.</param>
    /// <param name="matrixB">The second matrix to multiply.</param>
    /// <param name="threadCount">The number of threads to use for parallel multiplication.</param>
    /// <param name="runs">The number of times to run each multiplication for averaging.</param>
    /// <returns>A BenchmarkResult containing timing statistics for both approaches.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either matrix is null.</exception>
    /// <exception cref="ArgumentException">Thrown when matrices are incompatible for multiplication.</exception>
    public static BenchmarkResult RunBenchmark(int[,] matrixA, int[,] matrixB, int threadCount, int runs)
    {
        var sequentialTimes = new double[runs];
        var parallelTimes = new double[runs];

        for (int i = 0; i < runs; i++)
        {
            var watch = Stopwatch.StartNew();
            MatrixMultiplier.Multiply(matrixA, matrixB);
            sequentialTimes[i] = watch.Elapsed.TotalMilliseconds;
        }

        for (int i = 0; i < runs; i++)
        {
            var watch = Stopwatch.StartNew();
            MatrixMultiplier.MultiplyParallel(matrixA, matrixB, threadCount);
            parallelTimes[i] = watch.Elapsed.TotalMilliseconds;
        }

        return new(sequentialTimes, parallelTimes);
    }

    /// <summary>
    /// Represents the results of a matrix multiplication benchmark.
    /// </summary>
    public record BenchmarkResult(double[] SequentialTimes, double[] ParallelTimes)
    {
        /// <summary>
        /// Gets the mean execution time for sequential multiplication in milliseconds.
        /// </summary>
        public double SequentialMean => this.SequentialTimes.Average();

        /// <summary>
        /// Gets the mean execution time for parallel multiplication in milliseconds.
        /// </summary>
        public double ParallelMean => this.ParallelTimes.Average();

        /// <summary>
        /// Gets the speedup factor (sequential time / parallel time).
        /// </summary>
        public double SpeedUp => this.SequentialMean / this.ParallelMean;

        /// <summary>
        /// Gets the standard deviation of sequential execution times.
        /// </summary>
        public double SequentialStdDev => this.CalculateStdDev(this.SequentialTimes);

        /// <summary>
        /// Gets the standard deviation of parallel execution times.
        /// </summary>
        public double ParallelStdDev => this.CalculateStdDev(this.ParallelTimes);

        private double CalculateStdDev(double[] values)
        {
            double mean = values.Average();
            double sum = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sum / values.Length);
        }
    }
}
