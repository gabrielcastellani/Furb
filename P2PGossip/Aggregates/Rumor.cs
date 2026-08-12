namespace P2PGossip.Aggregates
{
    internal class Rumor
    {
        public required string Id { get; set; }
        public required string Origin { get; set; }
        public required string Text { get; set; }
        public int RemainingRounds { get; set; }
    }
}
