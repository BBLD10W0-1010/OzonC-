using System.Net.Http.Json;

namespace Products.Client;

internal static class HttpProductDemo
{
    public static async Task RunAsync(Uri address, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = address };
        var id = await CreateProductAsync(client, cancellationToken);
        Console.WriteLine($"Создан товар: {id}");

        var product = await GetJsonAsync(client, $"/api/products/{id}", cancellationToken);
        Console.WriteLine($"Товар по Id: {product}");

        const string query = "/api/products?type=Food&warehouseNumber=1&pageNumber=1&pageSize=5";
        var page = await GetJsonAsync(client, query, cancellationToken);
        Console.WriteLine($"Список с фильтрами и пагинацией: {page}");
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var request = new
        {
            name = "Молоко",
            price = 99.90m,
            weight = 1,
            type = "Food",
            warehouseNumber = 1
        };

        using var response = await client.PostAsJsonAsync("/api/products", request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var created = await response.Content.ReadFromJsonAsync<CreatedProduct>(cancellationToken);

        return created?.Id ?? throw new InvalidDataException("Сервер вернул пустой ответ на создание товара.");
    }

    private static async Task<string> GetJsonAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}");
    }

    private sealed record CreatedProduct(Guid Id);
}
