// <copyright file="Program.cs" company="Roman Levashev">
// Copyright (c) Roman Levashev. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using SimpleFTPClient;

string host = "localhost";
int port = 5252;

if (args.Length > 2)
{
    host = args[0];
    if (int.TryParse(args[1], out int parsedPort))
    {
        port = parsedPort;
    }
}
else if (args.Length == 1)
{
    host = args[0];
}

using var client = new Client(host, port);

try
{
    await client.ConnectAsync();
    Console.WriteLine($"Connected to {host}:{port}");
    Console.WriteLine("Type 'help' for commands\n");

    await InteractiveLoop(client);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

static async Task InteractiveLoop(Client client)
{
    var directory = await client.GetServerDirectory();

    while (true)
    {
        try
        {
            Console.Write($"ftp: {directory}> ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.ToLower() == "exit" || input.ToLower() == "quit")
            {
                break;
            }

            await ProcessCommand(client, input);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

static async Task ProcessCommand(Client client, string command)
{
    var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    switch (parts[0].ToLower())
    {
        case "help":
            ShowHelp();
            break;

        case "list":
        case "ls":
            if (parts.Length < 2)
            {
                Console.WriteLine("Usage: list <path>");
                return;
            }

            await ListCommand(client, parts[1]);
            break;

        case "get":
            if (parts.Length < 3)
            {
                Console.WriteLine("Usage: get <remote-path> <local-path>");
                return;
            }

            Console.WriteLine((await client.GetAsync(parts[1], parts[2])) ? "File Downloaded" : "Error");

            break;

        default:
            Console.WriteLine($"Unknown command: {parts[0]}");
            break;
    }

    static async Task ListCommand(Client client, string path)
    {
        try
        {
            var result = await client.ListAsync(path);

            if (!result.Success)
            {
                Console.WriteLine($"Directory not found: {path}");
                return;
            }

            Console.WriteLine($"Directory: {path}");
            Console.WriteLine($"Total: {result.Entries!.Count} items\n");

            foreach (var entry in result.Entries!)
            {
                string type = entry.IsDirectory ? "dir" : "file";
                Console.WriteLine($"{type} {entry.Name}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  list <path>      - List directory contents");
        Console.WriteLine("  get <remote> <local> - Download file");
        Console.WriteLine("  help            - Show this help");
        Console.WriteLine("  exit/quit       - Exit client");
    }
}