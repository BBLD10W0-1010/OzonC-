using ClientStat.Application.Models;

namespace ClientStat.Application.Interfaces{
    public interface IOrderRepository
    {
        IEnumerable<Order> GetOrders();
    }
}