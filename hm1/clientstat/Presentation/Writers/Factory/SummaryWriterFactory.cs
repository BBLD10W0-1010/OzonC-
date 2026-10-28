public class SummaryWriterFactory : ISummaryWriterFactory
{
    private readonly ConsoleSummaryWriter _consoleWriter;
    private readonly FileSummaryWriter _fileWriter;

    public SummaryWriterFactory(
        ConsoleSummaryWriter consoleWriter,
        FileSummaryWriter fileWriter)
    {
        _consoleWriter = consoleWriter;
        _fileWriter = fileWriter;
    }

    public ISummaryWriter Create(string mode)
    {
        return mode switch
        {
            "console" => _consoleWriter,
            "file" => _fileWriter,
            _ => throw new ArgumentException(
                $"Неизвестный режим '{mode}'")
        };
    }
}