using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Loopback, 5000);
listener.Start();

Console.WriteLine("Servidor ITERATIVO escutando na porta 5000");

while (true)
{
    using var client = await listener.AcceptTcpClientAsync();

    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] aceito: {client.Client.RemoteEndPoint}");

    var stream = client.GetStream();
    var buffer = new byte[1024];
    int n;

    while ((n = await stream.ReadAsync(buffer)) > 0)
        await stream.WriteAsync(buffer.AsMemory(0, n));

    Console.WriteLine("Cliente finalziado");
}