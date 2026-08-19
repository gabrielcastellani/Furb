using System.Net.Sockets;
using System.Text;

namespace SocketChat
{
    public static class ChatSession
    {
        private const string QuitCommand = "/quit";

        public static async Task RunAsync(Socket socket, string name, CancellationToken ct)
        {
            Console.WriteLine($"Connected to {socket.RemoteEndPoint}.");
            Console.WriteLine($"You are '{name}'. Type a message and press Enter. Type {QuitCommand} to leave.");
            Console.WriteLine();

            using var session = CancellationTokenSource.CreateLinkedTokenSource(ct);

            var receiving = ReceiveLoopAsync(socket, session);
            var sending = SendLoopAsync(socket, name, session);

            await Task.WhenAny(receiving, sending);
            session.Cancel();

            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }

            Console.WriteLine("Session closed.");
        }

        private static async Task ReceiveLoopAsync(Socket socket, CancellationTokenSource session)
        {
            try
            {
                while (!session.IsCancellationRequested)
                {
                    var frame = await Frames.ReadAsync(socket, session.Token);
                    if (frame is null)
                    {
                        Console.WriteLine("[peer left the conversation]");
                        return;
                    }

                    Console.WriteLine(Encoding.UTF8.GetString(frame));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[receive failed: {ex.GetType().Name} - {ex.Message}]");
            }
            finally
            {
                session.Cancel();
            }
        }

        private static async Task SendLoopAsync(Socket socket, string name, CancellationTokenSource session)
        {
            try
            {
                while (!session.IsCancellationRequested)
                {
                    var line = await Task.Run(Console.ReadLine);

                    if (line is null || line.Equals(QuitCommand, StringComparison.OrdinalIgnoreCase))
                        return;

                    if (line.Length == 0)
                        continue;

                    var payload = Encoding.UTF8.GetBytes($"{name}: {line}");
                    await Frames.WriteAsync(socket, payload, session.Token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[send failed: {ex.GetType().Name} - {ex.Message}]");
            }
            finally
            {
                session.Cancel();
            }
        }
    }
}
