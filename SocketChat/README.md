# SocketChat

Two-peer chat over a raw TCP socket. No relay server: one instance listens, the other connects.

## Run

Terminal 1:

    dotnet run -- listen 9000 alice

Terminal 2:

    dotnet run -- connect 127.0.0.1 9000 bob

Type a message and press Enter. Type `/quit` to leave.

## Files

| File | Role |
|---|---|
| `Program.cs` | Argument parsing and mode selection |
| `Connection.cs` | Listen / connect |
| `ChatSession.cs` | Full-duplex send and receive loops |
| `Frames.cs` | Length-prefixed message framing |

## Notes

Requires .NET 8 or later. Not compiled in the environment where it was generated; run `dotnet build` first.
