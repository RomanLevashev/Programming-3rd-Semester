// <copyright file="MatrixTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace MatrixTetsts;

using MathNet.Numerics.LinearAlgebra;
using ParallelMatrixMultiplication;

/// <summary>
/// Contains unit tests for matrix multiplication operations and file I/O functionality.
/// </summary>
[TestClass]
public sealed class MatrixTests
{
    private Random random = new();

    /// <summary>
    /// Tests that parallel multiplication of square integer matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplyParallelSquareIntegerMatricesResultsMatchMathNet() => this.MultiplyParallelTest(32, 32, 32);

    /// <summary>
    /// Tests that parallel multiplication of non-square integer matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplyParallelNotSquareIntegerMatricesResultsMatchMathNet() => this.MultiplyParallelTest(32, 64, 48);

    /// <summary>
    /// Tests that parallel multiplication of large matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplyParallelLargeMatricesResultsMatchMathNet() => this.MultiplyParallelTest(100, 80, 120);

    /// <summary>
    /// Tests that sequential multiplication of square integer matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplySquareIntegerMatricesResultsMatchMathNet() => this.MultiplyTest(32, 32, 32);

    /// <summary>
    /// Tests that sequential multiplication of non-square integer matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplyNotSquareIntegerMatricesResultsMatchMathNet() => this.MultiplyTest(32, 64, 48);

    /// <summary>
    /// Tests that sequential multiplication of large matrices produces results matching MathNet reference implementation.
    /// </summary>
    [TestMethod]
    public void MultiplyLargeMatricesResultsMatchMathNet() => this.MultiplyTest(100, 80, 120);

    /// <summary>
    /// Tests that parallel multiplication throws ArgumentException when matrices have incompatible dimensions.
    /// </summary>
    [TestMethod]
    public void MultiplyParallelIncompatibleMatrixSizesThrowsArgumentException()
    {
        var (matrixA, matrixB) = this.GetRandomMatrixPair(16, 28, 14, 32);

        Assert.ThrowsException<ArgumentException>(() => MatrixMultiplier.MultiplyParallel(matrixA, matrixB, 16));
    }

    /// <summary>
    /// Tests that sequential multiplication throws ArgumentException when matrices have incompatible dimensions.
    /// </summary>
    [TestMethod]
    public void MultiplyIncompatibleMatrixSizesThrowsArgumentException()
    {
        var (matrixA, matrixB) = this.GetRandomMatrixPair(16, 28, 14, 32);

        Assert.ThrowsException<ArgumentException>(() => MatrixMultiplier.Multiply(matrixA, matrixB));
    }

    /// <summary>
    /// Tests that matrix serialization to file and deserialization from file preserves all data correctly.
    /// </summary>
    [TestMethod]
    public void MatrixReadWriteTest()
    {
        var initMatrix = MatrixHandler.GenerateRandomMatrix(32, 32, -10000, 10000);
        MatrixHandler.SaveMatrixToFile(initMatrix, "matrixTest");
        var matrixFromFile = MatrixHandler.ReadMatrixFromFile("matrixTest");

        for (int i = 0; i < 32; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                Assert.AreEqual(initMatrix[i, j], matrixFromFile[i, j]);
            }
        }
    }

    private static double[,] ConvertToDouble(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        var result = new double[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = matrix[i, j];
            }
        }

        return result;
    }

    private void MultiplyParallelTest(int rowsA, int colsA, int colsB)
    {
        int threadCount = Environment.ProcessorCount;
        var (matrixA, matrixB) = this.GetRandomMatrixPair(rowsA, colsA, colsA, colsB);
        var result = MatrixMultiplier.MultiplyParallel(matrixA, matrixB, threadCount);

        var (mathNetMatrixA, mathNetMatrixB) = this.GetMathNetMatrixPair(matrixA, matrixB);
        Matrix<double> expected = mathNetMatrixA * mathNetMatrixB;

        this.AssertMatricesEqual(result, expected);
    }

    private void MultiplyTest(int rowsA, int colsA, int colsB)
    {
        var (matrixA, matrixB) = this.GetRandomMatrixPair(rowsA, colsA, colsA, colsB);
        var result = MatrixMultiplier.Multiply(matrixA, matrixB);

        var (mathNetMatrixA, mathNetMatrixB) = this.GetMathNetMatrixPair(matrixA, matrixB);
        Matrix<double> expected = mathNetMatrixA * mathNetMatrixB;

        this.AssertMatricesEqual(result, expected);
    }

    private void AssertMatricesEqual(int[,] actual, Matrix<double> expected)
    {
        int rows = actual.GetLength(0);
        int cols = actual.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Assert.AreEqual(actual[i, j], expected[i, j], 0.000001);
            }
        }
    }

    private (int[,] MatrixA, int[,] MatrixB) GetRandomMatrixPair(int rowsA, int colsA, int rowsB, int colsB) =>
        (MatrixHandler.GenerateRandomMatrix(rowsA, colsA, -10000, 10000), MatrixHandler.GenerateRandomMatrix(rowsB, colsB, -10000, 10000));

    private (Matrix<double> MatrixA, Matrix<double> MatrixB) GetMathNetMatrixPair(int[,] matrixA, int[,] matrixB) =>
        (Matrix<double>.Build.DenseOfArray(ConvertToDouble(matrixA)), Matrix<double>.Build.DenseOfArray(ConvertToDouble(matrixB)));
}
