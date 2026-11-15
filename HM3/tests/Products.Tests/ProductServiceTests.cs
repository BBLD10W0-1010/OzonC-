using FluentValidation;
using Products.Api.Application;
using Products.Api.Application.Validation;
using Products.Api.Domain;
using Products.Api.Infrastructure;

namespace Products.Tests;

public sealed class ProductServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StoresAllFieldsAndUsesServerUtcTime()
    {
        var service = CreateService(new InMemoryProductRepository());
        var id = service.Create(new("  Молоко  ", 99.90m, 1.5, ProductType.Food, 7));

        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(new Product(id, "Молоко", 99.90m, 1.5, ProductType.Food, Start, 7), service.GetById(id));
    }

    [Fact]
    public void GetById_MissingProduct_ThrowsNotFound()
    {
        var service = CreateService(new InMemoryProductRepository());
        Assert.Throws<ProductNotFoundException>(() => service.GetById(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(ProductType.General, 1)]
    [InlineData(ProductType.Food, 3)]
    [InlineData(ProductType.Electronics, 1)]
    [InlineData(ProductType.HouseholdChemicals, 0)]
    public void GetProducts_FiltersByTypeIncludingZeroEnumValue(ProductType type, int count)
    {
        var page = SeededService().GetProducts(new() { Type = type });
        Assert.Equal(count, page.TotalCount);
        Assert.All(page.Items, product => Assert.Equal(type, product.Type));
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    [InlineData(99, 0)]
    public void GetProducts_FiltersByWarehouse(int warehouse, int count)
    {
        var page = SeededService().GetProducts(new() { WarehouseNumber = warehouse });
        Assert.Equal(count, page.TotalCount);
        Assert.All(page.Items, product => Assert.Equal(warehouse, product.WarehouseNumber));
    }

    [Fact]
    public void GetProducts_DateBoundariesAreInclusive()
    {
        var page = SeededService().GetProducts(new() { CreatedFrom = Start.AddDays(1), CreatedTo = Start.AddDays(3) });
        Assert.Equal(new[] { 2, 3, 4 }, page.Items.Select(x => int.Parse(x.Name)));
    }

    [Fact]
    public void GetProducts_EqualDateBoundariesMatchExactInstant()
    {
        var page = SeededService().GetProducts(new() { CreatedFrom = Start, CreatedTo = Start });
        Assert.Equal("1", Assert.Single(page.Items).Name);
    }

    [Fact]
    public void GetProducts_DatesWithDifferentOffsetsCompareAsInstants()
    {
        var instant = Start.AddDays(1).ToOffset(TimeSpan.FromHours(5));
        var page = SeededService().GetProducts(new() { CreatedFrom = instant, CreatedTo = instant });
        Assert.Equal("2", Assert.Single(page.Items).Name);
    }

    [Fact]
    public void GetProducts_SupportsOpenEndedDateRanges()
    {
        Assert.Equal(2, SeededService().GetProducts(new() { CreatedFrom = Start.AddDays(3) }).TotalCount);
        Assert.Equal(2, SeededService().GetProducts(new() { CreatedTo = Start.AddDays(1) }).TotalCount);
    }

    [Fact]
    public void GetProducts_CombinesAllFiltersBeforePagination()
    {
        var page = SeededService().GetProducts(new()
        {
            CreatedFrom = Start.AddDays(1),
            CreatedTo = Start.AddDays(4),
            Type = ProductType.Food,
            WarehouseNumber = 1,
            PageSize = 1,
            PageNumber = 2
        });
        Assert.Equal("5", Assert.Single(page.Items).Name);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }

    [Theory]
    [InlineData(1, 2, "1,2", 3)]
    [InlineData(2, 2, "3,4", 3)]
    [InlineData(3, 2, "5", 3)]
    [InlineData(4, 2, "", 3)]
    [InlineData(int.MaxValue, 100, "", 1)]
    public void GetProducts_PaginatesWithoutOverflow(int number, int size, string expectedNames, int totalPages)
    {
        var page = SeededService().GetProducts(new() { PageNumber = number, PageSize = size });
        Assert.Equal(expectedNames, string.Join(",", page.Items.Select(x => x.Name)));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(totalPages, page.TotalPages);
        Assert.Equal(number, page.PageNumber);
        Assert.Equal(size, page.PageSize);
    }

    [Fact]
    public void GetProducts_EmptyStorageReturnsZeroPages()
    {
        var page = CreateService(new InMemoryProductRepository()).GetProducts(new());
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public void GetProducts_EqualCreationTimesUseIdForStablePaging()
    {
        var repository = new InMemoryProductRepository();
        var products = Enumerable.Range(1, 4).Select(i => MakeProduct(i, ProductType.General, 1) with { CreatedAt = Start }).ToArray();
        foreach (var product in products.Reverse()) repository.Add(product);
        var service = CreateService(repository);

        var first = service.GetProducts(new() { PageSize = 2 }).Items;
        var second = service.GetProducts(new() { PageSize = 2, PageNumber = 2 }).Items;
        Assert.Equal(products.Select(x => x.Id), first.Concat(second).Select(x => x.Id));
    }

    [Fact]
    public void Create_ParallelRequestsDoNotLoseProducts()
    {
        var service = CreateService(new InMemoryProductRepository());
        Parallel.For(0, 250, i => service.Create(new($"Товар {i}", 1m, 1, ProductType.General, 1)));
        var pages = Enumerable.Range(1, 3).Select(i => service.GetProducts(new() { PageNumber = i, PageSize = 100 })).ToArray();
        Assert.All(pages, page => Assert.Equal(250, page.TotalCount));
        Assert.Equal(250, pages.SelectMany(x => x.Items).Select(x => x.Id).Distinct().Count());
    }

    public static TheoryData<CreateProductRequest, string> InvalidProducts => new()
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
    public void Create_InvalidFieldsAreRejectedWithoutWriting(CreateProductRequest request, string field)
    {
        var repository = new InMemoryProductRepository();
        var exception = Assert.Throws<ValidationException>(() => CreateService(repository).Create(request));
        Assert.Contains(exception.Errors, error => error.PropertyName == field);
        Assert.Empty(repository.GetAll());
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
    public void GetProducts_InvalidQueryIsRejected(ProductQuery query) =>
        Assert.Throws<ValidationException>(() => SeededService().GetProducts(query));

    private static ProductService SeededService()
    {
        var repository = new InMemoryProductRepository();
        repository.Add(MakeProduct(1, ProductType.General, 1));
        repository.Add(MakeProduct(2, ProductType.Food, 1));
        repository.Add(MakeProduct(3, ProductType.Food, 2));
        repository.Add(MakeProduct(4, ProductType.Electronics, 2));
        repository.Add(MakeProduct(5, ProductType.Food, 1));
        return CreateService(repository);
    }

    private static Product MakeProduct(int i, ProductType type, int warehouse) =>
        new(Guid.Parse($"00000000-0000-0000-0000-{i:D12}"), i.ToString(), i, i, type, Start.AddDays(i - 1), warehouse);

    private static ProductService CreateService(InMemoryProductRepository repository) =>
        new(repository, new CreateProductValidator(), new ProductQueryValidator(), new FixedTimeProvider());

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Start;
    }
}
