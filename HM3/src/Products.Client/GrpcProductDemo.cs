using Grpc.Net.Client;
using Products.Contracts.Grpc;

namespace Products.Client;

internal static class GrpcProductDemo
{
    public static async Task RunAsync(Uri address, CancellationToken cancellationToken)
    {
        using var channel = GrpcChannel.ForAddress(address);
        var client = new ProductsService.ProductsServiceClient(channel);
        var request = new CreateProductRequest
        {
            Name = "Молоко",
            Price = "99.90",
            Weight = 1,
            Type = ProductType.Food,
            WarehouseNumber = 1
        };

        var created = await client.CreateProductAsync(request, cancellationToken: cancellationToken);
        Console.WriteLine($"Создан товар: {created.Id}");

        var product = await client.GetProductAsync(
            new GetProductRequest { Id = created.Id }, cancellationToken: cancellationToken);
        Console.WriteLine($"Товар по Id: {product}");

        var query = new GetProductsRequest
        {
            Type = ProductType.Food,
            WarehouseNumber = 1,
            PageNumber = 1,
            PageSize = 5
        };
        var page = await client.GetProductsAsync(query, cancellationToken: cancellationToken);
        Console.WriteLine($"Список с фильтрами и пагинацией: {page}");
    }
}
