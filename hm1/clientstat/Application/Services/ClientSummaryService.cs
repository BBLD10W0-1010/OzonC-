using System.Text.RegularExpressions;
using ClientStat.Application.Exceptions;
using ClientStat.Application.Interfaces;
using ClientStat.Application.Models;

namespace ClientStat.Application.Services
{
    public class ClientSummaryService : IClientSummaryService
    {
        private readonly IOrderRepository _orderRepository;

        public ClientSummaryService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public IEnumerable<ClientSummary> CreateSummary(IEnumerable<long> clientIds)
        {
            var orders = _orderRepository.GetOrders().ToList();

            foreach (var clientId in clientIds)
            {
                var clientOrders = orders.Where(x => x.ClientId == clientId).ToList();

                if (clientOrders.Count == 0)
                {
                    throw new ClientNotFoundException(clientId);
                }

                var clientSum = clientOrders.Sum(x => x.ItemQuantity * x.ItemPrice);

                var favoriteItemName = clientOrders.GroupBy(x => x.ItemName).Select(group => new
                {
                    Name = group.Key,
                    Quantity = group.Sum(x => x.ItemQuantity)
                })
                .OrderByDescending(x => x.Quantity)
                .First().Name;

                yield return new ClientSummary{ ClientId = clientId, OrdersSum = clientSum, FavoriteItemName = favoriteItemName};
            }
        }
    }
}
