namespace ClientStat.Application.Models
{
    public sealed record Order
    {
        public required long ClientId { get; init; }

        public required long OrderId { get; init; }

        public required string ItemName { get; init; }

        public required int ItemQuantity { get; init; }

        public required float ItemPrice { get; init; }
    }
}
