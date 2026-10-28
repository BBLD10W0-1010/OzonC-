using ClientStat.Application.Models;

public interface ISummaryWriter
{
    void Write(IEnumerable<ClientSummary> report);
}