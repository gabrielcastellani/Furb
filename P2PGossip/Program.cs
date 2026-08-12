using P2PGossip.Aggregates;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

const int Fanout = 2;
const int RoundIntervalMs = 1000;
const int DeathTimeoutSeconds = 8;
const int RumorRounds = 4;

if (args.Length < 1)
{
    Console.WriteLine("usage: dotnet run -- <my-port> [bootstrap-port]");
    return;
}

var myPort = int.Parse(args[0]);
var myId = $"node-{myPort}";
long myHeartbeat = 0;

var udp = new UdpClient(myPort);
var view = new ConcurrentDictionary<string, Peer>();
var rumors = new ConcurrentDictionary<string, Rumor>();

Console.WriteLine($"== {myId} online (UDP {myPort}) ==");
Console.WriteLine("   type text to inject a rumor | /peers | /quit");

if (args.Length > 1)
{
    var bootstrapPort = int.Parse(args[1]);
    var bootstrapId = $"node-{bootstrapPort}";
    view[bootstrapId] = new Peer
    {
        Id = bootstrapId,
        Port = bootstrapPort,
        Heartbeat = 0,
        LastSeen = DateTime.UtcNow
    };
    Console.WriteLine($"   bootstrap: {bootstrapId}");
}

_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            var result = await udp.ReceiveAsync();
            var packet = JsonSerializer.Deserialize<GossipDto>(Encoding.UTF8.GetString(result.Buffer));
            if (packet is not null) Merge(packet);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[recv error] {ex.Message}");
        }
    }
});

_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(RoundIntervalMs);
        myHeartbeat++;
        Prune();
        await Spread();
    }
});

while (true)
{
    var line = Console.ReadLine();
    if (line is null) { await Task.Delay(1000); continue; }

    line = line.Trim();
    if (line.Length == 0) continue;
    if (line == "/quit") break;

    if (line == "/peers")
    {
        Console.WriteLine($"-- view of {myId}: {view.Count} peers --");
        foreach (var p in view.Values.OrderBy(p => p.Port))
            Console.WriteLine($"   {p.Id}  hb={p.Heartbeat}  last seen {(DateTime.UtcNow - p.LastSeen).TotalSeconds:F1}s ago");
        continue;
    }

    var rumorId = Guid.NewGuid().ToString("N")[..6];
    rumors[rumorId] = new Rumor
    {
        Id = rumorId,
        Origin = myId,
        Text = line,
        RemainingRounds = RumorRounds
    };
    Console.WriteLine($"[rumor {rumorId} injected - will spread for {RumorRounds} rounds]");
}

void Merge(GossipDto gossip)
{
    ApplyPeer(gossip.SenderId, gossip.SenderPort, gossip.SenderHeartbeat);
    foreach (var p in gossip.Peers) ApplyPeer(p.Id, p.Port, p.Heartbeat);

    foreach (var r in gossip.Rumors)
    {
        if (!rumors.TryAdd(r.Id, new Rumor
        {
            Id = r.Id,
            Origin = r.Origin,
            Text = r.Text,
            RemainingRounds = RumorRounds
        })) continue;

        Console.WriteLine($"\n>>> [{r.Origin}] {r.Text}");
    }
}

void ApplyPeer(string id, int port, long heartbeat)
{
    if (id == myId) return;

    if (view.TryGetValue(id, out var peer))
    {
        if (heartbeat > peer.Heartbeat)
        {
            peer.Heartbeat = heartbeat;
            peer.LastSeen = DateTime.UtcNow;
        }
    }
    else
    {
        view[id] = new Peer { Id = id, Port = port, Heartbeat = heartbeat, LastSeen = DateTime.UtcNow };
        Console.WriteLine($"[+] discovered {id} (view: {view.Count})");
    }
}

void Prune()
{
    var now = DateTime.UtcNow;
    foreach (var peer in view.Values.ToList())
    {
        if ((now - peer.LastSeen).TotalSeconds > DeathTimeoutSeconds && view.TryRemove(peer.Id, out _))
            Console.WriteLine($"[-] {peer.Id} presumed DEAD (view: {view.Count})");
    }
}

async Task Spread()
{
    var peers = view.Values.ToList();
    if (peers.Count == 0) return;

    var packet = new GossipDto(
        myId,
        myPort,
        myHeartbeat,
        peers.Select(p => new PeerDto(p.Id, p.Port, p.Heartbeat)).ToList(),
        rumors.Values.Where(r => r.RemainingRounds > 0)
                     .Select(r => new RumorDto(r.Id, r.Origin, r.Text)).ToList());

    var bytes = JsonSerializer.SerializeToUtf8Bytes(packet);

    foreach (var target in peers.OrderBy(_ => Random.Shared.Next()).Take(Fanout))
    {
        try
        {
            await udp.SendAsync(bytes, new IPEndPoint(IPAddress.Loopback, target.Port));
        }
        catch { }
    }

    foreach (var r in rumors.Values)
        if (r.RemainingRounds > 0) r.RemainingRounds--;
}

//dotnet build
//cd bin/Debug/net8.0
//dotnet P2PGossip.dll 5001          # terminal 1 — não conhece ninguém
//dotnet P2PGossip.dll 5002 5001     # terminal 2 — bootstrap pelo 5001
//dotnet P2PGossip.dll 5003 5001     # terminal 3
//dotnet P2PGossip.dll 5004 5002     # terminal 4 — bootstrap pelo 5002, não pelo 5001
//dotnet P2PGossip.dll 5005 5003     # terminal 5