using ClientStat.Application.Exceptions;

public class ApplicationRunner
{
    private readonly IClientSummaryService _reportService;
    private readonly ISummaryWriterFactory _reportWriterFactory;

    public ApplicationRunner(
        IClientSummaryService reportService,
        ISummaryWriterFactory reportWriterFactory)
    {
        _reportService = reportService;
        _reportWriterFactory = reportWriterFactory;
    }

    public void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Использование:");
            Console.WriteLine("console 1010 1020");
            Console.WriteLine("file 1010 1020");
            return;
        }

        var mode = args[0];
        ISummaryWriter writer;

        try
        {
            writer = _reportWriterFactory.Create(mode);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        var clientIds = new List<long>();

        foreach (var argument in args.Skip(1))
        {
            if (!long.TryParse(argument, out var clientId))
            {
                Console.WriteLine($"Некорректный ID клиента '{argument}'. Ожидается 64-битное целое число.");
                return;
            }

            clientIds.Add(clientId);
        }

        try
        {
            var report = _reportService.CreateSummary(clientIds);
            writer.Write(report);
        }
        catch (ClientNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
