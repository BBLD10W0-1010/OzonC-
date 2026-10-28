using ClientStat.Application.Models;

public interface IClientSummaryService
{
    IEnumerable<ClientSummary> CreateSummary(IEnumerable<long> clientIds);
}