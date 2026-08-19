using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();

Console.WriteLine("[Async] Escutando em 9000");

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

while (!cts.IsCancellationRequested)
{
    var client = await listener.AcceptTcpClientAsync(cts.Token);
    _ = AtenderAsync(client, cts.Token);
}

static async Task AtenderAsync(TcpClient client, CancellationToken ct)
{
    try
    {
        Console.WriteLine($"[Async] Conexão iniciada: {client.Client.RemoteEndPoint}");

        using (client)
        await using (var stream = client.GetStream())
        {
            var buffer = new byte[4096];
            int n;
            while ((n = await stream.ReadAsync(buffer, ct)) > 0)
                await stream.WriteAsync(buffer.AsMemory(0, n), ct);

            Console.Error.WriteLine($"[Async] Conexão encerrada: {client.Client.RemoteEndPoint}");
        }
    }
    catch (OperationCanceledException) { }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"[Async] Conexão encerrada: {client.Client.RemoteEndPoint}");
    }
}