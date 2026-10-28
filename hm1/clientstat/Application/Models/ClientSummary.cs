namespace ClientStat.Application.Models{
    public class ClientSummary
    {
        public long ClientId { get; set; }
        public float OrdersSum { get; set; }
        public string FavoriteItemName { get; set; } = string.Empty;
    }
}