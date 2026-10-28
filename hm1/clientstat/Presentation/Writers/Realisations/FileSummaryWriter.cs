using ClientStat.Application.Models;

public class FileSummaryWriter : ISummaryWriter
{
   public void Write(IEnumerable<ClientSummary> report)
    {
        var fileName =
            $"report_{DateTime.Now:yyyyMMdd_HHmmssfff}.txt";

        var filePath = Path.GetFullPath(fileName);

        using (var writer = new StreamWriter(filePath))
        {
            writer.WriteLine(
                "client_id | orders_sum | favorite_item_name");

            foreach (var row in report)
            {
                writer.WriteLine(
                    $"{row.ClientId} | {row.OrdersSum:F2} | {row.FavoriteItemName}");
            }
        }

        Console.WriteLine($"Файл создан: {filePath}");
    }
}