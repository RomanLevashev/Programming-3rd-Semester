// <copyright file="Client.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace SimpleFTPClient;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Represents an FTP client for communicating with a SimpleFTPServer.
/// Provides methods for listing directory contents and downloading files.
/// </summary>
public class Client : IDisposable
{
    private static readonly int ReadBufferSize = 4096;
    private static readonly int FileBufferSize = 8 * 1024 * 1024;
    private readonly string host;
    private readonly int port;
    private TcpClient? client;
    private NetworkStream? stream;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="host">The hostname or IP address of the FTP server. Defaults to "localhost".</param>
    /// <param name="port">The port number of the FTP server. Must be between 1024 and 65535. Defaults to 5252.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="port"/> is not in the range 1024-65535.</exception>
    public Client(string host = "localhost", int port = 5252)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be empty", nameof(host));
        }

        if (port < 1024 || port > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be in range 1024-65535");
        }

        this.host = host;
        this.port = port;
    }

    /// <summary>
    /// Establishes a connection to the FTP server.
    /// </summary>
    /// <param name="token">Cancellation token that can be used to cancel the connection attempt.</param>
    /// <returns>A task representing the asynchronous connection operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the client was disposed.</exception>
    public async Task ConnectAsync(CancellationToken token = default)
    {
        if (this.client != null)
        {
            throw new InvalidOperationException("Already connected");
        }

        ObjectDisposedException.ThrowIf(this.disposed, this);

        this.client = new();

        await this.client.ConnectAsync(this.host, this.port, token);
        this.stream = this.client.GetStream();

        Console.WriteLine($"Connected to {this.host}:{this.port}");
    }

    /// <summary>
    /// Gets the current working directory on the server.
    /// </summary>
    /// <param name="token">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation, containing the server's current directory path.</returns>
    public async Task<string> GetServerDirectory(CancellationToken token = default)
    {
        this.EnsureConnected();
        await this.SendCommandAsync("3 .\n", token);
        return (await this.ReadUntilSeparator(token)).Response;
    }

    /// <summary>
    /// Lists the contents of a directory on the server.
    /// </summary>
    /// <param name="path">The path of the directory to list.</param>
    /// <param name="token">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing a tuple where:
    /// - Success indicates whether the operation was successful
    /// - Entries contains the list of directory entries if successful, null otherwise
    /// Each entry consists of a name and a boolean indicating whether it's a directory.
    /// </returns>
    /// <exception cref="FormatException">Thrown when the server response is malformed.</exception>
    public async Task<(bool Success, List<(string Name, bool IsDirectory)>? Entries)> ListAsync(string path, CancellationToken token = default)
    {
        this.EnsureConnected();

        var command = $"1 {path}\n";
        await this.SendCommandAsync(command, token);
        var (response, _) = await this.ReadUntilSeparator(token);

        if (response == "-1")
        {
            return (false, null);
        }

        var parts = response.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 1 || !int.TryParse(parts[0], out int count) || ((count * 2) + 1) != parts.Length)
        {
            throw new FormatException($"Invalid server response: {response}");
        }

        var entries = new List<(string Name, bool IsDirectory)>();

        for (int i = 0; i < parts.Length - 2; i += 2)
        {
            entries.Add((parts[1 + i], parts[2 + i] == "true" ? true : false));
        }

        return (true, entries);
    }

    /// <summary>
    /// Downloads a file from the server to a local path.
    /// </summary>
    /// <param name="remotePath">The path of the file on the server.</param>
    /// <param name="localPath">The local path where the file should be saved.</param>
    /// <param name="token">Cancellation token that can be used to cancel the download.</param>
    /// <returns>A task that represents the asynchronous operation, containing true if the download was successful.</returns>
    /// <exception cref="FormatException">Thrown when the server sends invalid size information.</exception>
    /// <exception cref="IncompleteDataException">Thrown when the connection is closed before the entire file is received.</exception>
    public async Task<bool> GetAsync(string remotePath, string localPath, CancellationToken token = default)
    {
        this.EnsureConnected();
        using var file = File.Open(localPath, FileMode.Create);

        string command = $"2 {remotePath}\n";
        await this.SendCommandAsync(command, token);
        var (response, remainder) = await this.ReadUntilSeparator(token, ' ');

        if (!long.TryParse(response, out long size))
        {
            throw new FormatException("Server sent invalid data");
        }

        if (size == -1)
        {
            return false;
        }

        await file.WriteAsync(remainder, token);

        var buffer = new byte[FileBufferSize];
        long totalBytesRead = remainder.Length;

        while (!token.IsCancellationRequested && totalBytesRead < size)
        {
            int bytesRead = await this.stream!.ReadAsync(buffer, token);

            if (bytesRead == 0)
            {
                throw new IncompleteDataException($"Connection closed before receiving file.");
            }

            totalBytesRead += bytesRead;

            await file.WriteAsync(buffer.AsMemory()[..bytesRead], token);
        }

        return true;
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Client"/>.
    /// </summary>
    public async void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.client?.Dispose();
        this.client = null;
        this.disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task SendCommandAsync(string command, CancellationToken token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(command);
        await this.stream!.WriteAsync(bytes, token);
        await this.stream.FlushAsync(token);
    }

    private async Task<(string Response, byte[] Remainder)> ReadUntilSeparator(CancellationToken token, char separator = '\n')
    {
        var buffer = new byte[ReadBufferSize];
        var recievedDataBuilder = new StringBuilder();
        while (!token.IsCancellationRequested)
        {
            int bytesRead;

            try
            {
                bytesRead = await this.stream!.ReadAsync(buffer, token);
            }
            catch (IOException ex) when (ex.InnerException is SocketException se &&
                                         se.SocketErrorCode == SocketError.ConnectionReset)
            {
                throw new ConnectionClosedException("Connection was forcibly closed by remote host", ex);
            }

            if (bytesRead == 0)
            {
                throw new IncompleteDataException(
                $"Connection closed before receiving separator. " +
                $"Remaining data: '{recievedDataBuilder.ToString()}'");
            }

            recievedDataBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            var recievedData = recievedDataBuilder.ToString();
            var separatorIndex = recievedData.IndexOf(separator);

            if (separatorIndex == -1)
            {
                continue;
            }

            return (recievedData[..separatorIndex], buffer.AsMemory()[(separatorIndex + 1)..bytesRead].ToArray());
        }

        throw new OperationCanceledException(token);
    }

    private void EnsureConnected()
    {
        if (this.client == null || !this.client.Connected)
        {
            throw new InvalidOperationException("Not connected to server");
        }
    }
}
