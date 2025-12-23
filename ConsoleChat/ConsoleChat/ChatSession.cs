// <copyright file="ChatSession.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>
namespace ConsoleChat;

using System.Net.Sockets;
using System.Text;

/// <summary>
/// Handles a single bidirectional chat session over a TCP connection.
/// </summary>
public class ChatSession
{
    private readonly TcpClient client;
    private readonly TextReader input;
    private readonly TextWriter output;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSession"/> class.
    /// </summary>
    /// <param name="client">TCP client connected to the chat peer.</param>
    /// <param name="input">Input source for outgoing messages.</param>
    /// <param name="output">Output target for incoming messages.</param>
    public ChatSession(TcpClient client, TextReader input, TextWriter output)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.input = input ?? throw new ArgumentNullException(nameof(input));
        this.output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Starts the bidirectional chat session and runs until cancellation or disconnection.
    /// </summary>
    /// <param name="cancellationToken">Optional token to stop the session.</param>
    /// <returns>A task that completes when the session ends.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var stream = this.client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

        var receiveTask = this.ReceiveLoopAsync(reader, linkedCts);
        var sendTask = this.SendLoopAsync(writer, linkedCts);

        try
        {
            await Task.WhenAll(receiveTask, sendTask);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            this.client.Close();
        }
    }

    private static bool IsExit(string? message) =>
        string.Equals(message, "exit", StringComparison.OrdinalIgnoreCase);

    private async Task ReceiveLoopAsync(StreamReader reader, CancellationTokenSource sessionCts)
    {
        while (!sessionCts.IsCancellationRequested)
        {
            string? message;
            try
            {
                message = await reader.ReadLineAsync(sessionCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (message is null)
            {
                sessionCts.Cancel();
                break;
            }

            if (IsExit(message))
            {
                await this.output.WriteLineAsync("Peer ended the chat.");
                sessionCts.Cancel();
                break;
            }

            await this.output.WriteLineAsync($"Peer: {message}");
        }
    }

    private async Task SendLoopAsync(StreamWriter writer, CancellationTokenSource sessionCts)
    {
        while (!sessionCts.IsCancellationRequested)
        {
            string? message;
            try
            {
                message = await this.input.ReadLineAsync(sessionCts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (message is null)
            {
                break;
            }

            await writer.WriteLineAsync(message);

            if (IsExit(message))
            {
                sessionCts.Cancel();
                break;
            }
        }
    }
}
