using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Loopback, 5000);
listener.Start();

Console.WriteLine("Servidor CONCORRENTE escutando na porta 5000");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleAsync(client);
}

static async Task HandleAsync(TcpClient client)
{
    using (client)
    {
        Console.WriteLine($"Aceito: {client.Client.RemoteEndPoint} " +
                          $"(thread {Environment.CurrentManagedThreadId})");
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            int n;
            while ((n = await stream.ReadAsync(buffer)) > 0)
                await stream.WriteAsync(buffer.AsMemory(0, n));
        }
        catch (IOException) { /* cliente derrubou a conexão */ }
        
        Console.WriteLine("Cliente saiu");
    }
}