// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using SimpleFTPServer;

int port = 5252;

if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
{
    port = parsedPort;
}

Console.WriteLine($"Starting FTP server on port {port}");
Console.WriteLine("Press Ctrl+C to stop");

using var server = new Server(port);

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("\nShutting down...");
    server.Dispose();
};

try
{
    await server.Start();
}
catch (OperationCanceledException)
{
    Console.WriteLine("Server stopped gracefully");
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    return 1;
}

return 0;