using System.Net;
using System.Net.Sockets;

namespace SocketChat
{
    public static class Connection
    {
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

        public static async Task<Socket> ListenAsync(int port, CancellationToken ct)
        {
            using var listener = new Socket(
                addressFamily: AddressFamily.InterNetwork,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp);

            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Listen(1);

            Console.WriteLine($"Listening on {listener.LocalEndPoint}. Waiting for a peer...");

            var peer = await listener.AcceptAsync(ct);
            peer.NoDelay = true;
            return peer;
        }

        public static async Task<Socket> ConnectAsync(string host, int port, CancellationToken ct)
        {
            var socket = new Socket(
                addressFamily: AddressFamily.InterNetwork,
                socketType: SocketType.Stream,
                protocolType: ProtocolType.Tcp)
            { NoDelay = true };

            Console.WriteLine($"Connecting to {host}:{port}...");

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(ConnectTimeout);
                await socket.ConnectAsync(host, port, timeout.Token);
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }
}
