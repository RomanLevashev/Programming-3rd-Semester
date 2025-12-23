// <copyright file="ChatSessionTests.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace ConsoleChat.Tests;

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ConsoleChat;
using NUnit.Framework;

/// <summary>
/// Integration-style tests for <see cref="ChatSession"/> behavior.
/// </summary>
public class ChatSessionTests
{
    /// <summary>
    /// Ensures that when a local user sends "exit" the remote session ends and reports it.
    /// </summary>
    /// <returns>A task representing the async test flow.</returns>
    [Test]
    public async Task LocalExitStopsRemoteSession()
    {
        var (serverClient, client) = await CreateConnectedClientsAsync();
        var serverOutput = new StringWriter();
        var clientOutput = new StringWriter();

        var serverSession = new ChatSession(serverClient, new StringReader("exit\n"), serverOutput);
        var clientSession = new ChatSession(client, new StringReader(string.Empty), clientOutput);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(serverSession.RunAsync(cts.Token), clientSession.RunAsync(cts.Token));

        Assert.That(clientOutput.ToString(), Does.Contain("Peer ended the chat."));
    }

    /// <summary>
    /// Verifies that a message sent by one peer is delivered to the other.
    /// </summary>
    /// <returns>A task representing the async test flow.</returns>
    [Test]
    public async Task MessagesFlowBetweenPeers()
    {
        var (serverClient, client) = await CreateConnectedClientsAsync();
        var serverOutput = new StringWriter();
        var clientOutput = new StringWriter();

        var serverSession = new ChatSession(serverClient, new StringReader(string.Empty), serverOutput);
        var clientSession = new ChatSession(client, new StringReader("hi there\nexit\n"), clientOutput);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(serverSession.RunAsync(cts.Token), clientSession.RunAsync(cts.Token));

        Assert.That(serverOutput.ToString(), Does.Contain("Peer: hi there"));
    }

    /// <summary>
    /// Confirms that exit detection is case-insensitive.
    /// </summary>
    /// <returns>A task representing the async test flow.</returns>
    [Test]
    public async Task ExitIsCaseInsensitive()
    {
        var (serverClient, client) = await CreateConnectedClientsAsync();
        var serverOutput = new StringWriter();
        var clientOutput = new StringWriter();

        var serverSession = new ChatSession(serverClient, new StringReader(string.Empty), serverOutput);
        var clientSession = new ChatSession(client, new StringReader("ExIt\n"), clientOutput);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Task.WhenAll(serverSession.RunAsync(cts.Token), clientSession.RunAsync(cts.Token));

        Assert.That(serverOutput.ToString(), Does.Contain("Peer ended the chat."));
    }

    /// <summary>
    /// Checks that an already-cancelled token causes the session to exit immediately.
    /// </summary>
    /// <returns>A task representing the async test flow.</returns>
    [Test]
    public async Task AlreadyCancelledTokenExitsImmediately()
    {
        var (serverClient, client) = await CreateConnectedClientsAsync();
        var serverOutput = new StringWriter();
        var serverSession = new ChatSession(serverClient, new StringReader(string.Empty), serverOutput);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await serverSession.RunAsync(cts.Token);

        Assert.That(serverOutput.ToString(), Is.Empty);
    }

    private static async Task<(TcpClient Server, TcpClient Client)> CreateConnectedClientsAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var client = new TcpClient();
        var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
        var serverAcceptTask = listener.AcceptTcpClientAsync();

        await Task.WhenAll(connectTask, serverAcceptTask);
        listener.Stop();

        return (await serverAcceptTask, client);
    }
}
