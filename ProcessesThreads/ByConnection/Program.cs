using System.Net;
using System.Net.Sockets;

var listener = new TcpListener(IPAddress.Any, 9000);
listener.Start();

Console.WriteLine("[By Connection] Escutando em 9000");

while (true)
{
    var client = listener.AcceptTcpClient();
    var thread = new Thread(() => Answer(client));
    thread.IsBackground = true;
    thread.Start();
}

static void Answer(TcpClient client)
{
    Console.WriteLine($"[By Connection] Conexão iniciada: {client.Client.RemoteEndPoint}");

    using var stream = client.GetStream();
    var buffer = new byte[4096];
    int n;

    while ((n = stream.Read(buffer)) > 0)
        stream.Write(buffer, 0, n);

    Console.Error.WriteLine($"[By Connection] Conexão encerrada: {client.Client.RemoteEndPoint}");
}