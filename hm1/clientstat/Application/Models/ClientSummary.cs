namespace ClientStat.Application.Models
{
    public sealed record ClientSummary
    {
        public required long ClientId { get; init; }

        public required float OrdersSum { get; init; }

        public required string FavoriteItemName { get; init; }
    }
}
