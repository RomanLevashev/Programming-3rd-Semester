// <copyright file="Server.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPServer;

using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// A simple FTP server for handling file operations.
/// </summary>
public class Server : IDisposable
{
    private readonly int port;
    private readonly CancellationToken token;
    private readonly CancellationTokenSource cts;

    private TcpListener? listener;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Server"/> class.
    /// </summary>
    /// <param name="port">The port number to listen on. Must be between 1024 and 65535.</param>
    /// <param name="externalToken">External cancellation token that can trigger server shutdown.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when port is not in range 1024-65535.</exception>
    public Server(int port = 5252, CancellationToken externalToken = default)
    {
        if (port > 65535 || port < 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be in range 1024-65535");
        }

        this.port = port;
        this.cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
        this.token = this.cts.Token;
    }

    /// <summary>
    /// Gets or sets the size of file chunks when transferring files. Default is 8 MB.
    /// </summary>
    public int FileChunkSize { get; set; } = 8 * 1024 * 1024;

    /// <summary>
    /// Gets or sets the buffer size for receiving data from clients. Default is 4096 bytes.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// Starts the FTP server and begins accepting client connections.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when server was disposed.</exception>
    /// <returns>A task that represents the asynchronous server operation.</returns>
    public async Task Start()
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        this.listener = new(IPAddress.Any, this.port);
        this.listener.Start();

        Console.WriteLine($"Server started on port {this.port}");
        await Task.Run(async () =>
        {
            while (!this.token.IsCancellationRequested)
            {
                try
                {
                    var client = await this.listener.AcceptTcpClientAsync(this.token);
                    _ = Task.Run(async () => await this.HandleClient(client));
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Server is shutting down.");
                    break;
                }
            }
        });
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Server"/>.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.cts.Cancel();
        this.listener!.Stop();
        this.cts.Dispose();
        this.listener.Dispose();
        GC.SuppressFinalize(this);
        this.disposed = true;
    }

    private static async Task SendResponse(Stream stream, string response, CancellationToken token = default)
    {
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, token);
    }

    private async Task HandleClient(TcpClient client)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);

        using (client)
        await using (var stream = client.GetStream())
        {
            var buffer = new byte[this.ReceiveBufferSize];
            var accumulatedData = new List<byte>();

            while (!this.token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, this.token);
                if (bytesRead == 0)
                {
                    break;
                }

                accumulatedData.AddRange(buffer.AsSpan()[0..bytesRead]);

                while (true)
                {
                    int commandEndIndex = accumulatedData.IndexOf((byte)'\n');
                    if (commandEndIndex == -1)
                    {
                        break;
                    }

                    string commandLine = Encoding.UTF8.GetString([.. accumulatedData[0..(commandEndIndex + 1)]]);
                    accumulatedData.RemoveRange(0, commandEndIndex + 1);

                    await this.ProcessCommand(commandLine.TrimEnd('\n'), stream);
                }
            }
        }
    }

    private async Task ProcessCommand(string command, Stream stream)
    {
        var tokens = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length != 2)
        {
            await SendResponse(stream, $"Invalid command format. Expected: '<command> <path>', got: '{command}'\n", this.token);
            return;
        }

        if (!int.TryParse(tokens[0], out int firstPart) || firstPart > 3 || firstPart < 1)
        {
            await SendResponse(stream, $"Invalid command format:\nYour request: {command}\n Expected Format: (1 or 2) <path: String>\n", this.token);
            return;
        }

        var path = tokens[1];

        switch (firstPart)
        {
            case 1:
                await this.HandleListRequest(path, stream);
                break;

            case 2:
                await this.HandleGetRequest(path, stream);
                break;

            case 3:
                await SendResponse(stream, $"{Directory.GetCurrentDirectory()}\n");
                break;
        }
    }

    private async Task HandleListRequest(string path, Stream stream)
    {
        if (!Directory.Exists(path))
        {
            await SendResponse(stream, "-1\n", this.token);
            return;
        }

        var directory = new DirectoryInfo(path);
        var entries = directory.GetFileSystemInfos();
        var responseBuilder = new StringBuilder();

        responseBuilder.Append(entries.Length);
        responseBuilder.Append(' ');

        foreach (var entry in entries)
        {
            responseBuilder.Append($"{entry.Name} {(entry is DirectoryInfo ? "true" : "false")} ");
        }

        responseBuilder.Append('\n');

        await SendResponse(stream, responseBuilder.ToString(), this.token);
    }

    private async Task HandleGetRequest(string path, Stream stream)
    {
        if (!File.Exists(path))
        {
            await SendResponse(stream, "-1 \n", this.token);
            return;
        }

        var fileInfo = new FileInfo(path);
        var size = fileInfo.Length;

        var sizeBytes = Encoding.UTF8.GetBytes($"{size} ");
        await stream.WriteAsync(sizeBytes);

        using (var fileStream = File.OpenRead(path))
        {
            var buffer = new byte[this.FileChunkSize];
            int bytesRead = 0;

            while ((bytesRead = await fileStream.ReadAsync(buffer)) != 0)
            {
                await stream.WriteAsync(buffer.AsMemory()[..bytesRead], this.token);
            }
        }
    }
}
