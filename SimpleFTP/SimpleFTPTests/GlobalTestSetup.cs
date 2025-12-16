// <copyright file="GlobalTestSetup.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPTests;

/// <summary>
/// Provides global setup and cleanup for all tests in the assembly.
/// Creates and manages a temporary directory with test files for FTP testing.
/// </summary>
[TestClass]
public class GlobalTestSetup
{
    /// <summary>
    /// Gets the global test directory path created for all tests.
    /// </summary>
    public static string GlobalTestDirectory { get; private set; } = null!;

    /// <summary>
    /// Initializes the test environment before all tests in the assembly execute.
    /// Creates a unique temporary directory with test file structure.
    /// </summary>
    /// <param name="context">Test context providing information about the test run.</param>
    [AssemblyInitialize]
    public static void AssemblyInitialize(TestContext context)
    {
        GlobalTestDirectory = Path.Combine(
            Path.GetTempPath(),
            $"FtpGlobalTest_{Guid.NewGuid()}");

        Directory.CreateDirectory(GlobalTestDirectory);

        CreateGlobalTestStructure();

        context.Properties["GlobalTestDirectory"] = GlobalTestDirectory;
    }

    /// <summary>
    /// Cleans up the test environment after all tests in the assembly complete.
    /// Deletes the temporary directory and all its contents.
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        if (Directory.Exists(GlobalTestDirectory))
        {
            Directory.Delete(GlobalTestDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Creates the directory and file structure for FTP testing.
    /// Includes text files, nested directories, and empty directories.
    /// </summary>
    private static void CreateGlobalTestStructure()
    {
        File.WriteAllText(
            Path.Combine(GlobalTestDirectory, "file1.txt"),
            "This is test file 1 content");

        File.WriteAllText(
            Path.Combine(GlobalTestDirectory, "file2.txt"),
            "This is test file 2 with longer content");

        var subDir1 = Path.Combine(GlobalTestDirectory, "subdir1");
        Directory.CreateDirectory(subDir1);
        File.WriteAllText(
            Path.Combine(subDir1, "nested1.txt"),
            "Nested file 1");

        var subDir2 = Path.Combine(GlobalTestDirectory, "subdir2");
        Directory.CreateDirectory(subDir2);
        File.WriteAllText(
            Path.Combine(subDir2, "nested2.txt"),
            "Nested file 2");

        Directory.CreateDirectory(
            Path.Combine(GlobalTestDirectory, "empty_dir"));
    }
}
