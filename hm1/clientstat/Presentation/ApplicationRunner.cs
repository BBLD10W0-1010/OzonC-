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

        var clientIds = args
            .Skip(1)
            .Select(long.Parse)
            .ToList();

        try
        {
            var report = _reportService.CreateSummary(clientIds);

            var writer = _reportWriterFactory.Create(mode);

            writer.Write(report);
        }
        catch (ClientNotFoundException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}