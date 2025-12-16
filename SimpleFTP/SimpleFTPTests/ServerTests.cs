// <copyright file="ServerTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPTests.ServerTests;

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleFTPServer;

/// <summary>
/// Contains unit tests for the FTP server functionality.
/// Tests individual server methods without network communication.
/// </summary>
[TestClass]
public class ServerTests
{
    private static Server server = null!;

    private static string TestDirectory => GlobalTestSetup.GlobalTestDirectory;

    /// <summary>
    /// Initializes the test environment before all tests in the class execute.
    /// Creates a server instance for testing.
    /// </summary>
    /// <param name="context">Test context providing information about the test run.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        server = new Server();
    }

    /// <summary>
    /// Tests that the server constructor throws ArgumentOutOfRangeException for invalid port values.
    /// </summary>
    [TestMethod]
    public void Server_Constructor_WithInvalidPort_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Server(80));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Server(70000));
    }

    /// <summary>
    /// Tests that HandleListRequest returns correct format for an existing directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_HandleListRequest_WithExistingDirectory_ShouldReturnCorrectFormat()
    {
        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "HandleListRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, [TestDirectory, responseStream])!;

        responseStream.Position = 0;
        var response = Encoding.UTF8.GetString(responseStream.ToArray());

        Assert.EndsWith("\n", response);

        var parts = response.Trim().Split(' ');
        Assert.IsTrue(int.TryParse(parts[0], out int count));
        Assert.AreEqual(5, count);
        Assert.HasCount(1 + (count * 2), parts);
    }

    /// <summary>
    /// Tests that HandleListRequest returns "-1" for a non-existent directory.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_HandleListRequest_WithNonExistentDirectory_ShouldReturnMinusOne()
    {
        using var cts = new CancellationTokenSource();
        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "HandleListRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, [
            Path.Combine(TestDirectory, "non_existent"),
            responseStream
        ])!;

        responseStream.Position = 0;
        var response = Encoding.UTF8.GetString(responseStream.ToArray());
        Assert.AreEqual("-1", response.Trim());
    }

    /// <summary>
    /// Tests that HandleGetRequest correctly sends file content with size prefix.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_HandleGetRequest_WithExistingFile_ShouldSendFileContent()
    {
        var testFilePath = Path.Combine(TestDirectory, "file1.txt");
        var fileContent = "This is test file 1 content";
        File.WriteAllText(testFilePath, fileContent);

        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "HandleGetRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, [testFilePath, responseStream])!;

        responseStream.Position = 0;

        var sizeBytes = new byte[100];
        int bytesRead = responseStream.Read(sizeBytes);
        var sizeString = Encoding.UTF8.GetString(sizeBytes, 0, bytesRead);

        var sizeAndRemainder = sizeString.Split(' ');
        var sizePart = sizeAndRemainder[0];
        var remainder = string.Join(' ', sizeAndRemainder[1..]);
        Assert.IsTrue(long.TryParse(sizePart, out long size));
        Assert.AreEqual(fileContent.Length, size);

        var remainingBytes = new byte[responseStream.Length - responseStream.Position];
        responseStream.Read(remainingBytes);
        var content = remainder + Encoding.UTF8.GetString(remainingBytes);

        Assert.AreEqual(fileContent, content);
    }

    /// <summary>
    /// Tests that HandleGetRequest returns "-1" for a non-existent file.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_HandleGetRequest_WithNonExistentFile_ShouldReturnMinusOne()
    {
        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "HandleGetRequest",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, [
            Path.Combine(TestDirectory, "non_existent1.txt"),
            responseStream,
        ])!;

        responseStream.Position = 0;
        var response = Encoding.UTF8.GetString(responseStream.ToArray());
        Assert.AreEqual("-1 \n", response);
    }

    /// <summary>
    /// Tests that ProcessCommand with command "3 ." returns the current directory path.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_ProcessCommand_Command3_ShouldReturnCurrentDirectory()
    {
        var originalDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(TestDirectory);

            var responseStream = new MemoryStream();

            var method = typeof(Server).GetMethod(
                "ProcessCommand",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            await (Task)method!.Invoke(server, ["3 .", responseStream])!;

            responseStream.Position = 0;
            var response = Encoding.UTF8.GetString(responseStream.ToArray());
            Assert.AreEqual(TestDirectory + "\n", response);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    /// <summary>
    /// Tests that ProcessCommand returns an error message for invalid command format.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_ProcessCommand_WithInvalidCommand_ShouldReturnErrorMessage()
    {
        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, ["invalid command", responseStream])!;

        responseStream.Position = 0;
        var response = Encoding.UTF8.GetString(responseStream.ToArray());
        Assert.Contains("Invalid command format", response);
    }

    /// <summary>
    /// Tests that ProcessCommand returns an error message for invalid command number.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task Server_ProcessCommand_WithInvalidCommandNumber_ShouldReturnErrorMessage()
    {
        var responseStream = new MemoryStream();

        var method = typeof(Server).GetMethod(
            "ProcessCommand",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        await (Task)method!.Invoke(server, ["5 somepath", responseStream])!;

        responseStream.Position = 0;
        var response = Encoding.UTF8.GetString(responseStream.ToArray());
        Assert.Contains("Invalid command format", response);
    }
}