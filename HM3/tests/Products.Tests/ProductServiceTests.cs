using FluentValidation;
using Products.Api.Application;
using Products.Api.Application.Validation;
using Products.Api.Domain;

namespace Products.Tests;

public sealed class ProductServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);
    private readonly StubRepository _repository = new();
    private readonly ProductService _service;

    public ProductServiceTests() => _service =
        new(_repository, new ProductDetailsValidator(), new ProductQueryValidator(), new FixedTimeProvider());

    [Fact]
    public async Task Create_StoresAllFieldsAndUsesServerUtcTime()
    {
        var id = await _service.CreateAsync(new("  Молоко  ", 99.90m, 1.5, ProductType.Food, 7), default);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(new Product(id, "Молоко", 99.90m, 1.5, ProductType.Food, Start, 7), _repository.Product);
        Assert.Equal(_repository.Product, await _service.GetByIdAsync(id, default));
    }

    [Fact]
    public async Task GetById_MissingProductThrowsNotFound()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Update_TrimsNameAndReturnsUpdatedProduct()
    {
        var id = await _service.CreateAsync(new("Молоко", 99.90m, 1, ProductType.Food, 7), default);

        var updated = await _service.UpdateAsync(id, new("  Чай  ", 199m, 0.2, ProductType.Food, 9), default);

        Assert.Equal(new Product(id, "Чай", 199m, 0.2, ProductType.Food, Start, 9), updated);
        Assert.Equal(updated, _repository.Product);
    }

    [Fact]
    public async Task Update_MissingProductThrowsNotFound()
    {
        await Assert.ThrowsAsync<ProductNotFoundException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new("Чай", 199m, 0.2, ProductType.Food, 9), default));
    }

    [Fact]
    public async Task Update_InvalidFieldsLeaveProductUnchanged()
    {
        var id = await _service.CreateAsync(new("Молоко", 99.90m, 1, ProductType.Food, 7), default);
        var original = _repository.Product;

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.UpdateAsync(id, new("Чай", -1, 0.2, ProductType.Food, 9), default));

        Assert.Same(original, _repository.Product);
    }

    [Theory]
    [InlineData(0, 50, 1, 0)]
    [InlineData(12, 4, 2, 3)]
    [InlineData(12, 5, 4, 3)]
    [InlineData(int.MaxValue, 100, int.MaxValue, 21474837)]
    public async Task GetProducts_ComputesMetadataFromRepositoryCount(
        int count, int size, int number, int expectedPages)
    {
        _repository.TotalCount = count;
        var query = new ProductQuery { PageSize = size, PageNumber = number, Type = ProductType.General };

        var page = await _service.GetProductsAsync(query, default);

        Assert.Same(query, _repository.Query);
        Assert.Equal(count, page.TotalCount);
        Assert.Equal(number, page.PageNumber);
        Assert.Equal(size, page.PageSize);
        Assert.Equal(expectedPages, page.TotalPages);
    }

    public static TheoryData<ProductDetails, string> InvalidProducts => new()
    {
        { new("", 1, 1, ProductType.General, 1), "Name" },
        { new("   ", 1, 1, ProductType.General, 1), "Name" },
        { new(null!, 1, 1, ProductType.General, 1), "Name" },
        { new(new string('a', 201), 1, 1, ProductType.General, 1), "Name" },
        { new("Товар", 0, 1, ProductType.General, 1), "Price" },
        { new("Товар", -1, 1, ProductType.General, 1), "Price" },
        { new("Товар", 1, 0, ProductType.General, 1), "Weight" },
        { new("Товар", 1, -1, ProductType.General, 1), "Weight" },
        { new("Товар", 1, double.NaN, ProductType.General, 1), "Weight" },
        { new("Товар", 1, double.PositiveInfinity, ProductType.General, 1), "Weight" },
        { new("Товар", 1, 1, (ProductType)99, 1), "Type" },
        { new("Товар", 1, 1, ProductType.General, 0), "WarehouseNumber" }
    };
    [Theory]
    [MemberData(nameof(InvalidProducts))]
    public async Task Create_InvalidFieldsAreRejectedWithoutWriting(ProductDetails request, string field)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request, default));

        Assert.Contains(exception.Errors, error => error.PropertyName == field);
        Assert.Null(_repository.Product);
    }

    public static TheoryData<ProductQuery> InvalidQueries => new()
    {
        new() { PageNumber = 0 }, new() { PageNumber = -1 },
        new() { PageSize = 0 }, new() { PageSize = -1 }, new() { PageSize = 101 },
        new() { WarehouseNumber = 0 }, new() { WarehouseNumber = -1 },
        new() { Type = (ProductType)99 },
        new() { CreatedFrom = Start.AddDays(1), CreatedTo = Start }
    };
    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public async Task GetProducts_InvalidQueryIsRejectedBeforeReading(ProductQuery query)
    {
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetProductsAsync(query, default));

        Assert.Null(_repository.Query);
    }

    private sealed class StubRepository : IProductRepository
    {
        public Product? Product { get; private set; }
        public ProductQuery? Query { get; private set; }
        public int TotalCount { get; set; }

        public Task<Guid> AddAsync(Product product, CancellationToken cancellationToken)
        {
            Product = product;
            return Task.FromResult(product.Id);
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Product?.Id == id ? Product : null);

        public Task<Product?> UpdateAsync(Guid id, ProductDetails details, CancellationToken cancellationToken)
        {
            if (Product?.Id != id)
            {
                return Task.FromResult<Product?>(null);
            }

            Product = new Product(id, details.Name, details.Price, details.Weight,
                details.Type, Product.CreatedAt, details.WarehouseNumber);
            return Task.FromResult<Product?>(Product);
        }

        public Task<(IReadOnlyList<Product> Items, int TotalCount)> FindAsync(
            ProductQuery query, CancellationToken cancellationToken)
        {
            Query = query;
            return Task.FromResult<(IReadOnlyList<Product>, int)>(([], TotalCount));
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Start;
    }
}
