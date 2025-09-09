// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using ParallelMatrixMultiplication;

var testSizes = new[]
{
    (100, 100, 100),
    (500, 500, 500),
    (1000, 800, 600),
    (200, 2000, 300),
};

int runs = 5;
int threadCount = Environment.ProcessorCount;

foreach (var (rowsA, colsA, colsB) in testSizes)
{
    Console.WriteLine($"Testing {rowsA} X {colsA} * {colsA} X {colsB}");

    var matrixA = MatrixHandler.GenerateRandomMatrix(rowsA, colsA, -10000, 10000);
    var matrixB = MatrixHandler.GenerateRandomMatrix(colsA, colsB, -10000, 10000);

    var result = MatrixBenchmark.RunBenchmark(matrixA, matrixB, threadCount, runs);

    Console.WriteLine($"Size: {rowsA}x{colsA} * {colsA}x{colsB}");
    Console.WriteLine($"Sequential mean: {result.SequentialMean:F2}ms (+-{result.SequentialStdDev:F2}ms)");
    Console.WriteLine($"Parallel mean:   {result.ParallelMean:F2}ms (+-{result.ParallelStdDev:F2}ms)");
    Console.WriteLine($"Speedup:    {result.SpeedUp:F2}");
    Console.WriteLine();
}