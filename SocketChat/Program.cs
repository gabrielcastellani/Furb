using SocketChat;
using System.Net.Sockets;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "listen":
            {
                var port = ReadInt(args, 1, 9000);
                var name = ReadText(args, 2, "peer-a");
                using var socket = await Connection.ListenAsync(port, cts.Token);
                await ChatSession.RunAsync(socket, name, cts.Token);
                return 0;
            }

        case "connect":
            {
                var host = ReadText(args, 1, "127.0.0.1");
                var port = ReadInt(args, 2, 9000);
                var name = ReadText(args, 3, "peer-b");
                using var socket = await Connection.ConnectAsync(host, port, cts.Token);
                await ChatSession.RunAsync(socket, name, cts.Token);
                return 0;
            }

        default:
            PrintUsage();
            return 1;
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled.");
    return 0;
}
catch (SocketException ex)
{
    Console.Error.WriteLine($"Socket error: {ex.SocketErrorCode} - {ex.Message}");
    return 1;
}

static int ReadInt(string[] args, int index, int fallback) =>
    args.Length > index && int.TryParse(args[index], out var value) ? value : fallback;

static string ReadText(string[] args, int index, string fallback) =>
    args.Length > index && !string.IsNullOrWhiteSpace(args[index]) ? args[index] : fallback;

static void PrintUsage() => Console.WriteLine("""
    Socket chat between two peers.

      chat listen  [port] [name]
      chat connect [host] [port] [name]

    Example:
      chat listen 9000 alice
      chat connect 127.0.0.1 9000 bob
    """);