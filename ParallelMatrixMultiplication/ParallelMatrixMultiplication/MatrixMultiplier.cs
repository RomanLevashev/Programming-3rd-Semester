// <copyright file="MatrixMultiplier.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ParallelMatrixMultiplication;

/// <summary>
/// Provides methods for matrix multiplication, including sequential and parallel implementations.
/// </summary>
public class MatrixMultiplier
{
    /// <summary>
    /// Multiplies two matrices using parallel computation.
    /// </summary>
    /// <param name="a">The first matrix to multiply.</param>
    /// <param name="b">The second matrix to multiply.</param>
    /// <param name="threadCount">The number of threads to use for parallel computation.</param>
    /// <returns>The product of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either matrix is null.</exception>
    /// <exception cref="ArgumentException">Thrown when matrices are incompatible for multiplication.</exception>
    public static int[,] MultiplyParallel(int[,] a, int[,] b, int threadCount)
    {
        ArgumentNullException.ThrowIfNull(a, nameof(a));
        ArgumentNullException.ThrowIfNull(b, nameof(b));

        if (a.GetLength(1) != b.GetLength(0))
        {
            throw new ArgumentException(
                "Matrices are not compatible for multiplication (the number of columns of a must be equal to the number of rows of b).");
        }

        int rowsA = a.GetLength(0);
        int colsB = b.GetLength(1);
        threadCount = Math.Min(threadCount, rowsA);

        int[,] result = new int[rowsA, colsB];

        Thread[] threads = new Thread[threadCount];

        for (int threadIndex = 0; threadIndex < threadCount; threadIndex++)
        {
            int localThreadIndex = threadIndex;
            threads[threadIndex] = new Thread(() =>
            {
                MultiplyStridedRows(a, b, result, threadCount, localThreadIndex);
            });
            threads[threadIndex].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return result;
    }

    /// <summary>
    /// Multiplies two matrices using sequential computation.
    /// </summary>
    /// <param name="a">The first matrix to multiply.</param>
    /// <param name="b">The second matrix to multiply.</param>
    /// <returns>The product of the two matrices.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either matrix is null.</exception>
    /// <exception cref="ArgumentException">Thrown when matrices are incompatible for multiplication.</exception>
    public static int[,] Multiply(int[,] a, int[,] b)
    {
        ArgumentNullException.ThrowIfNull(a, nameof(a));
        ArgumentNullException.ThrowIfNull(b, nameof(b));

        if (a.GetLength(1) != b.GetLength(0))
        {
            throw new ArgumentException(
                "Matrices are not compatible for multiplication (the number of columns of a must be equal to the number of rows of b).");
        }

        int rowsA = a.GetLength(0);
        int colsB = b.GetLength(1);
        int[,] result = new int[rowsA, colsB];

        MultiplyStridedRows(a, b, result, 1, 0);

        return result;
    }

    private static void MultiplyStridedRows(int[,] a, int[,] b, int[,] result, int stride, int startIndex)
    {
        int rowsA = a.GetLength(0);
        int colsA = a.GetLength(1);
        int colsB = b.GetLength(1);

        for (int currentRow = startIndex; currentRow < rowsA; currentRow += stride)
        {
            for (int currentCol = 0; currentCol < colsB; currentCol++)
            {
                int temp = 0;

                for (int i = 0; i < colsA; i++)
                {
                    temp += a[currentRow, i] * b[i, currentCol];
                }

                result[currentRow, currentCol] = temp;
            }
        }
    }
}
