using System.Globalization;
using ClientStat.Application.Interfaces;
using ClientStat.Application.Models;

public class CsvOrderRepository : IOrderRepository
{
    private readonly string _filePath;

    public CsvOrderRepository()
    {
        _filePath = Path.Combine(
            AppContext.BaseDirectory,
            "orders_log.csv");
    }

    public IEnumerable<Order> GetOrders()
    {
        var orderLines = File.ReadAllLines(_filePath);
        var orders = new List<Order>();

        foreach (var orderLine in orderLines.Skip(1))
        {
            var values = orderLine.Split(';');

            orders.Add(new Order
            {
                ClientId = long.Parse(values[0]),
                OrderId = long.Parse(values[1]),
                ItemName = values[2],
                ItemQuantity = int.Parse(values[3]),
                ItemPrice = float.Parse(values[4], CultureInfo.InvariantCulture)
            });
        }

        return orders;
    }
}