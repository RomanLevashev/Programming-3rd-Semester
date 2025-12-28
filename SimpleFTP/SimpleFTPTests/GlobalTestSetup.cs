// <copyright file="GlobalTestSetup.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPTests;

using System.Threading;

/// <summary>
/// Provides helpers to create and clean isolated test directories for FTP scenarios.
/// Each test should call creation/cleanup to avoid cross-test interference under parallel runs.
/// </summary>
public static class GlobalTestSetup
{
    /// <summary>
    /// Creates a unique temporary directory with the default test file structure.
    /// </summary>
    /// <returns>The full path to the created test directory.</returns>
    public static string CreateTestDirectory()
    {
        string testDirectory = Path.Combine(
            Path.GetTempPath(),
            $"FtpTest_{Guid.NewGuid()}");

        Directory.CreateDirectory(testDirectory);
        CreateTestStructure(testDirectory);
        return testDirectory;
    }

    /// <summary>
    /// Deletes a test directory and all its contents with retries to tolerate lingering handles.
    /// </summary>
    /// <param name="directory">The directory to delete.</param>
    public static void CleanupTestDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        const int maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                Directory.Delete(directory, recursive: true);
                return;
            }
            catch (IOException)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (attempt == maxAttempts)
                {
                    break;
                }

                Thread.Sleep(200);
            }
            catch (UnauthorizedAccessException)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (attempt == maxAttempts)
                {
                    break;
                }

                Thread.Sleep(200);
            }
        }

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Creates the directory and file structure for FTP testing.
    /// Includes text files, nested directories, and empty directories.
    /// </summary>
    /// <param name="rootDirectory">The root directory to populate.</param>
    private static void CreateTestStructure(string rootDirectory)
    {
        File.WriteAllText(
            Path.Combine(rootDirectory, "file1.txt"),
            "This is test file 1 content");

        File.WriteAllText(
            Path.Combine(rootDirectory, "file2.txt"),
            "This is test file 2 with longer content");

        var subDir1 = Path.Combine(rootDirectory, "subdir1");
        Directory.CreateDirectory(subDir1);
        File.WriteAllText(
            Path.Combine(subDir1, "nested1.txt"),
            "Nested file 1");

        var subDir2 = Path.Combine(rootDirectory, "subdir2");
        Directory.CreateDirectory(subDir2);
        File.WriteAllText(
            Path.Combine(subDir2, "nested2.txt"),
            "Nested file 2");

        Directory.CreateDirectory(
            Path.Combine(rootDirectory, "empty_dir"));
    }
}
