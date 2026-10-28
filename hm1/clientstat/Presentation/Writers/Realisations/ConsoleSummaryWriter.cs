using ClientStat.Application.Models;

public class ConsoleSummaryWriter : ISummaryWriter
{
    public void Write(IEnumerable<ClientSummary> report)
    {
        Console.WriteLine(
            "client_id | orders_sum | favorite_item_name");

        foreach (var row in report)
        {
            Console.WriteLine(
                $"{row.ClientId} | {row.OrdersSum:F2} | {row.FavoriteItemName}");
        }
    }
}