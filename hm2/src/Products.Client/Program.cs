using System.Text;
using System.Text.Json;
using Grpc.Core;
using Products.Client;

Console.OutputEncoding = Encoding.UTF8;

if (!ClientOptions.TryParse(args, out var options))
{
    Console.Error.WriteLine("Использование: Products.Client [http|grpc] [http(s)://адрес сервера]");
    return 2;
}

try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    Console.WriteLine($"Режим: {options.Mode}; сервер: {options.Address}");

    if (options.Mode == "grpc")
    {
        await GrpcProductDemo.RunAsync(options.Address, timeout.Token);
    }
    else
    {
        await HttpProductDemo.RunAsync(options.Address, timeout.Token);
    }

    return 0;
}
catch (Exception exception) when (exception is HttpRequestException or RpcException or
                                    OperationCanceledException or JsonException or InvalidDataException)
{
    Console.Error.WriteLine($"Не удалось выполнить запрос: {exception.Message}");
    Console.Error.WriteLine("Убедитесь, что Products.Api запущен и выбран правильный порт.");
    return 1;
}
