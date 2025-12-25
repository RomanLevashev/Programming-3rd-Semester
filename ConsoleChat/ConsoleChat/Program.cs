// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using System.Net;
using System.Net.Sockets;
using ConsoleChat;

/// <summary>
/// Entry point and CLI handling for the console chat application.
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out var port))
            {
                PrintUsage();
                return;
            }

            await RunServerAsync(port);
        }
        else if (args.Length == 2)
        {
            if (!int.TryParse(args[1], out var port))
            {
                PrintUsage();
                return;
            }

            await RunClientAsync(args[0], port);
        }
        else
        {
            PrintUsage();
        }

        static async Task RunServerAsync(int port)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"Server listening on port {port}. Waiting for a client...");

            using var client = await listener.AcceptTcpClientAsync();
            Console.WriteLine($"Client connected from {client.Client.RemoteEndPoint}. Type messages, 'exit' to quit.");

            var session = new ChatSession(client, Console.In, Console.Out);
            await session.RunAsync();

            Console.WriteLine("Connection closed. Shutting down server.");
            listener.Stop();
        }

        static async Task RunClientAsync(string host, int port)
        {
            using var client = new TcpClient();
            Console.WriteLine($"Connecting to {host}:{port} ...");
            try
            {
                await client.ConnectAsync(host, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                return;
            }

            Console.WriteLine("Connected. Type messages, 'exit' to quit.");

            var session = new ChatSession(client, Console.In, Console.Out);
            await session.RunAsync();

            Console.WriteLine("Connection closed. Exiting client.");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ConsoleChat <port>              # start server on port");
            Console.WriteLine("  ConsoleChat <ip> <port>         # start client and connect to server");
        }
    }
}
