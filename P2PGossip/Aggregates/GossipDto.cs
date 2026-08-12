namespace P2PGossip.Aggregates
{
    record GossipDto(
        string SenderId,
        int SenderPort,
        long SenderHeartbeat,
        List<PeerDto> Peers,
        List<RumorDto> Rumors);
}
