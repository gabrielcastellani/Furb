using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();

Console.WriteLine("[Iterativo] Escutando em 9000");

while (true)
{
    using var client = listener.AcceptTcpClient();
    Answer(client);
}

static void Answer(TcpClient client)
{
    Console.WriteLine($"[Iterativo] Conexão iniciada: {client.Client.RemoteEndPoint}");

    using var stream = client.GetStream();
    var buffer = new byte[4096];
    int n;

    while ((n = stream.Read(buffer)) > 0)
        stream.Write(buffer, 0, n);

    Console.Error.WriteLine($"[Iterativo] Conexão encerrada: {client.Client.RemoteEndPoint}");
}