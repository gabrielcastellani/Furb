namespace P2PGossip.Aggregates
{
    internal class Peer
    {
        public required string Id { get; set; }
        public required int Port { get; set; }
        public long Heartbeat { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
