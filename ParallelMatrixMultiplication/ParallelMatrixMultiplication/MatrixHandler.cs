// <copyright file="MatrixHandler.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ParallelMatrixMultiplication;

using System.Text;

/// <summary>
/// Provides methods for generating, saving, and loading integer matrices.
/// </summary>
public class MatrixHandler
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a matrix with specified dimensions filled with random integers.
    /// </summary>
    /// <param name="rows">Number of rows in the matrix.</param>
    /// <param name="columns">Number of columns in the matrix.</param>
    /// <param name="minValue">Inclusive lower bound of the random numbers.</param>
    /// <param name="maxValue">Exclusive upper bound of the random numbers.</param>
    /// <returns>A new matrix filled with random integers.</returns>
    /// <exception cref="ArgumentException">Thrown when rows or columns are negative.</exception>
    public static int[,] GenerateRandomMatrix(int rows, int columns, int minValue, int maxValue)
    {
        if (rows < 0 || columns < 0)
        {
            throw new ArgumentException("The matrix sizes must be positive");
        }

        var matrix = new int[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                matrix[i, j] = Random.Next(minValue, maxValue);
            }
        }

        return matrix;
    }

    /// <summary>
    /// Saves a matrix to a text file with space-separated values.
    /// </summary>
    /// <param name="matrix">The matrix to save.</param>
    /// <param name="path">The file path where the matrix will be saved.</param>
    /// <exception cref="ArgumentNullException">Thrown when the matrix is null.</exception>
    public static void SaveMatrixToFile(int[,] matrix, string path)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        StringBuilder currentLine = new();

        using (StreamWriter writer = new(path, false, Encoding.UTF8))
        {
            for (int i = 0; i < rows; i++)
            {
                currentLine.Clear();
                for (int j = 0; j < columns; j++)
                {
                    currentLine.Append(matrix[i, j]);

                    if (j < columns - 1)
                    {
                        currentLine.Append(' ');
                    }
                }

                writer.WriteLine(currentLine.ToString());
            }
        }
    }

    /// <summary>
    /// Loads a matrix from a text file with space-separated values.
    /// </summary>
    /// <param name="path">The file path to load the matrix from.</param>
    /// <returns>A matrix containing the values from the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file is empty.</exception>
    /// <exception cref="FormatException">Thrown when the file contains invalid data or inconsistent row lengths.</exception>
    public static int[,] ReadMatrixFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The file was not found at the specified path.");
        }

        string[] lines = File.ReadAllLines(path);

        if (lines.Length == 0)
        {
            throw new ArgumentException("File is empty.");
        }

        string[] firstLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int rows = lines.Length;
        int columns = firstLine.Length;

        int[,] matrix = new int[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            string[] currentLine = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (currentLine.Length != columns)
            {
                throw new FormatException("The rows have different numbers of elements.");
            }

            for (int j = 0; j < columns; j++)
            {
                if (int.TryParse(currentLine[j], out var current))
                {
                    matrix[i, j] = current;
                }
                else
                {
                    throw new FormatException($"There is no number in the {i}{j} position");
                }
            }
        }

        return matrix;
    }
}
