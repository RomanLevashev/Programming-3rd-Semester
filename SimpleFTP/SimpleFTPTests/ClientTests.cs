// <copyright file="ClientTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPTests.ClientTests;

using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleFTPClient;
using SimpleFTPServer;

/// <summary>
/// Contains integration tests for the FTP client functionality.
/// </summary>
[TestClass]
public class ClientTests
{
    private static readonly object PortLock = new();
    private static int portCounter = 6000;
#pragma warning disable CS8618
    private Server server;
    private Client client;
    private CancellationTokenSource cts;
#pragma warning restore CS8618
    private int port;

    private static string TestDirectory => GlobalTestSetup.GlobalTestDirectory;

    /// <summary>
    /// Initializes the test environment before each test method execution.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestInitialize]
    public async Task TestInitialize()
    {
        this.cts = new CancellationTokenSource();

        lock (PortLock)
        {
            this.port = portCounter++;
            if (portCounter > 65000)
            {
                portCounter = 6000;
            }
        }

        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(TestDirectory);

            this.server = new Server(this.port);
            _ = this.server.Start();

            await Task.Delay(200);

            this.client = new Client("localhost", this.port);
            await this.client.ConnectAsync(this.cts.Token);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    /// <summary>
    /// Cleans up test resources after each test method execution.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        this.cts?.Cancel();
        this.client?.Dispose();
        this.server?.Dispose();
    }

    /// <summary>
    /// Tests that attempting to connect to the server multiple times throws an InvalidOperationException.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task DoubleConnectShouldThrow()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => this.client.ConnectAsync(this.cts.Token));
    }

    /// <summary>
    /// Tests that ListAsync returns the correct directory structure including files and subdirectories.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListAsyncShouldReturnCorrectDirectoryStructure()
    {
        var (success, entries) = await this.client.ListAsync(TestDirectory, this.cts.Token);

        Assert.IsTrue(success);
        Assert.IsNotNull(entries);
        Assert.HasCount(5, entries);

        var fileNames = entries.Where(e => !e.IsDirectory).Select(e => e.Name).ToHashSet();
        var dirNames = entries.Where(e => e.IsDirectory).Select(e => e.Name).ToHashSet();

        Assert.Contains("file1.txt", fileNames);
        Assert.Contains("file2.txt", fileNames);
        Assert.Contains("subdir1", dirNames);
        Assert.Contains("subdir2", dirNames);
        Assert.Contains("empty_dir", dirNames);
    }

    /// <summary>
    /// Tests that ListAsync correctly lists files within a nested subdirectory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListAsyncWithNestedDirectoryShouldReturnSingleFile()
    {
        var subDirPath = Path.Combine(TestDirectory, "subdir1");

        var (success, entries) = await this.client.ListAsync(subDirPath, this.cts.Token);

        Assert.IsTrue(success);
        Assert.IsNotNull(entries);
        Assert.HasCount(1, entries);
        Assert.AreEqual("nested1.txt", entries[0].Name);
        Assert.IsFalse(entries[0].IsDirectory);
    }

    /// <summary>
    /// Tests that ListAsync returns an empty list when querying an empty directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListAsyncWithEmptyDirectoryShouldReturnEmptyList()
    {
        var emptyDirPath = Path.Combine(TestDirectory, "empty_dir");

        var (success, entries) = await this.client.ListAsync(emptyDirPath, this.cts.Token);

        Assert.IsTrue(success);
        Assert.IsNotNull(entries);
        Assert.IsEmpty(entries);
    }

    /// <summary>
    /// Tests that ListAsync returns failure when querying a non-existent directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListAsyncWithNonexistentDirectoryShouldFail()
    {
        var invalidPath = Path.Combine(TestDirectory, "nonexistent");

        var (success, entries) = await this.client.ListAsync(invalidPath, this.cts.Token);

        Assert.IsFalse(success);
        Assert.IsNull(entries);
    }

    /// <summary>
    /// Tests that ListAsync returns failure when provided with a file path instead of a directory path.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListAsyncWithFilePathShouldFail()
    {
        var filePath = Path.Combine(TestDirectory, "file1.txt");

        var (success, entries) = await this.client.ListAsync(filePath, this.cts.Token);

        Assert.IsFalse(success);
        Assert.IsNull(entries);
    }

    /// <summary>
    /// Tests that GetAsync successfully downloads a file and preserves its content.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task GetAsyncShouldDownloadFileWithCorrectContent()
    {
        var remotePath = Path.Combine(TestDirectory, "file1.txt");
        var localPath = Path.GetTempFileName();

        try
        {
            var result = await this.client.GetAsync(remotePath, localPath, this.cts.Token);

            Assert.IsTrue(result);

            var originalContent = await File.ReadAllTextAsync(remotePath);
            var downloadedContent = await File.ReadAllTextAsync(localPath);

            Assert.AreEqual("This is test file 1 content", originalContent);
            Assert.AreEqual(originalContent, downloadedContent);
        }
        finally
        {
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
        }
    }

    /// <summary>
    /// Tests that GetAsync successfully downloads a file from a nested subdirectory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task GetAsyncShouldDownloadNestedFile()
    {
        var remotePath = Path.Combine(TestDirectory, "subdir1", "nested1.txt");
        var localPath = Path.GetTempFileName();

        try
        {
            var result = await this.client.GetAsync(remotePath, localPath, this.cts.Token);

            Assert.IsTrue(result);

            var downloadedContent = await File.ReadAllTextAsync(localPath);
            Assert.AreEqual("Nested file 1", downloadedContent);
        }
        finally
        {
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
        }
    }

    /// <summary>
    /// Tests that GetAsync returns false when attempting to download a non-existent file.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task GetAsyncWithNonexistentFileShouldFail()
    {
        var remotePath = Path.Combine(TestDirectory, "nonexistent2.txt");
        var localPath = Path.GetTempFileName();

        try
        {
            var result = await this.client.GetAsync(remotePath, localPath, this.cts.Token);

            Assert.IsFalse(result);
        }
        finally
        {
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }
        }
    }

    /// <summary>
    /// Tests that GetServerDirectory returns a valid, existing directory path.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task GetServerDirectoryShouldReturnValidPath()
    {
        var directory = await this.client.GetServerDirectory(this.cts.Token);

        Assert.IsNotNull(directory);
        Assert.IsTrue(Directory.Exists(directory));
    }

    /// <summary>
    /// Tests that multiple concurrent ListAsync operations execute successfully without interference.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task MultipleConcurrentListOperationsShouldWork()
    {
        var tasks = new Task[5];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var (success, entries) = await this.client.ListAsync(TestDirectory, this.cts.Token);
                Assert.IsTrue(success);
                Assert.IsNotNull(entries);
            });
        }

        await Task.WhenAll(tasks);
    }
}