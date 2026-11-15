using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Products.Api.Application;
using Products.Api.Domain;
using Products.Api.Infrastructure;

namespace Products.Tests;

[Collection("Postgres")]
public sealed class PostgresProductRepositoryTests(PostgresFixture database) : IAsyncLifetime
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 1, 10, 15, 0, TimeSpan.Zero);
    private readonly IProductRepository _repository = database.Api.Services.GetRequiredService<IProductRepository>();

    public Task InitializeAsync() => database.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task InsertReturnsIdAndProductCanBeReadThroughAnotherConnection()
    {
        var product = NewProduct(1) with { Name = "O'Reilly", Price = 123456.123456m };
        Assert.Equal(product.Id, await _repository.AddAsync(product, default));

        await using var dataSource = NpgsqlDataSource.Create(database.ConnectionString);
        var reader = new PostgresProductRepository(dataSource);

        Assert.Equal(product, await reader.GetByIdAsync(product.Id, default));
        Assert.Null(await reader.GetByIdAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task FindFiltersBeforePagingAndUsesIdToBreakTies()
    {
        var first = NewProduct(1);
        var second = NewProduct(2);
        var last = NewProduct(3) with { CreatedAt = CreatedAt.AddHours(1) };
        Product[] products =
        [
            last, second, first,
            NewProduct(4) with { WarehouseNumber = 2 },
            NewProduct(5) with { Type = ProductType.Food },
            NewProduct(6) with { CreatedAt = CreatedAt.AddHours(-1) },
            NewProduct(7) with { CreatedAt = CreatedAt.AddHours(2) }
        ];
        foreach (var product in products)
        {
            await _repository.AddAsync(product, default);
        }

        var firstPage = await FindPageAsync(1);
        var secondPage = await FindPageAsync(2);
        var lastPage = await FindPageAsync(3);
        var beyondEnd = await FindPageAsync(int.MaxValue);

        Assert.Equal(first, Assert.Single(firstPage.Items));
        Assert.Equal(second, Assert.Single(secondPage.Items));
        Assert.Equal(last, Assert.Single(lastPage.Items));
        Assert.Empty(beyondEnd.Items);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(3, lastPage.TotalCount);
        Assert.Equal(3, beyondEnd.TotalCount);

        var upperBound = await _repository.FindAsync(new() { CreatedTo = CreatedAt.ToOffset(TimeSpan.FromHours(5)) }, default);
        Assert.Equal(5, upperBound.TotalCount);
        var lowerBound = await _repository.FindAsync(new() { CreatedFrom = last.CreatedAt }, default);
        Assert.Equal(2, lowerBound.TotalCount);
    }

    [Fact]
    public async Task UpdatePersistsAllEditableFieldsAndPreservesIdAndCreationTime()
    {
        var original = NewProduct(1);
        await _repository.AddAsync(original, default);
        var details = new ProductDetails("Чай", 250.40m, 0.1, ProductType.Food, 9);
        var expected = new Product(original.Id, "Чай", 250.40m, 0.1, ProductType.Food, original.CreatedAt, 9);

        Assert.Equal(expected, await _repository.UpdateAsync(original.Id, details, default));
        Assert.Equal(expected, await _repository.GetByIdAsync(original.Id, default));
        Assert.Null(await _repository.UpdateAsync(Guid.NewGuid(), details, default));

        var oldMatches = await _repository.FindAsync(new() { Type = original.Type }, default);
        var newMatches = await _repository.FindAsync(new() { Type = details.Type, WarehouseNumber = 9 }, default);
        Assert.Empty(oldMatches.Items);
        Assert.Equal(expected, Assert.Single(newMatches.Items));
    }

    private Task<(IReadOnlyList<Product> Items, int TotalCount)> FindPageAsync(int page) =>
        _repository.FindAsync(new()
        {
            Type = ProductType.General,
            WarehouseNumber = 1,
            CreatedFrom = CreatedAt.ToOffset(TimeSpan.FromHours(5)),
            CreatedTo = CreatedAt.AddHours(1),
            PageNumber = page,
            PageSize = 1
        }, default);

    private static Product NewProduct(int id) =>
        new(new Guid(id, 0, 0, new byte[8]), $"Товар {id}", 99.90m, 1, ProductType.General, CreatedAt, 1);
}
