using FluentValidation;
using Products.Api.Application;
using Products.Api.Application.Validation;
using Products.Api.Domain;
using Products.Api.Infrastructure;

namespace Products.Tests;

public sealed class ProductSearchTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<ProductQuery> Queries => new()
    {
        new() { PageNumber = 1, PageSize = 1 },
        new() { PageNumber = 50, PageSize = 10 },
        new() { PageNumber = 15, PageSize = 100 },
        new() { PageNumber = 16, PageSize = 100 },
        new() { PageNumber = int.MaxValue, PageSize = 100 },
        new() { Type = ProductType.General, PageNumber = 2, PageSize = 7 },
        new() { WarehouseNumber = 3, PageNumber = 2, PageSize = 11 },
        new()
        {
            Type = ProductType.Food,
            WarehouseNumber = 2,
            CreatedFrom = Start.AddDays(2),
            CreatedTo = Start.AddDays(8),
            PageNumber = 2,
            PageSize = 3
        },
        new() { CreatedFrom = Start.AddDays(20) },
        new() { Type = ProductType.Electronics, WarehouseNumber = 99 }
    };

    [Theory]
    [MemberData(nameof(Queries))]
    public void GetProducts_IndexedSearchMatchesFullSortReference(ProductQuery query)
    {
        var products = CreateCatalog();
        var repository = new InMemoryProductRepository();
        foreach (var product in products)
        {
            repository.Add(product);
        }
        var service = CreateService(repository);

        var expected = products
            .Where(product => !query.Type.HasValue || product.Type == query.Type)
            .Where(product => !query.WarehouseNumber.HasValue || product.WarehouseNumber == query.WarehouseNumber)
            .Where(product => !query.CreatedFrom.HasValue || product.CreatedAt >= query.CreatedFrom)
            .Where(product => !query.CreatedTo.HasValue || product.CreatedAt <= query.CreatedTo)
            .OrderBy(product => product.CreatedAt)
            .ThenBy(product => product.Id)
            .ToArray();

        var page = service.GetProducts(query);
        var offset = ((long)query.PageNumber - 1) * query.PageSize;
        var expectedItems = offset >= expected.Length
            ? []
            : expected.Skip((int)offset).Take(query.PageSize).ToArray();

        Assert.Equal(expectedItems, page.Items);
        Assert.Equal(expected.Length, page.TotalCount);
        Assert.Equal((int)Math.Ceiling(expected.Length / (double)query.PageSize), page.TotalPages);
        Assert.Equal(query.PageNumber, page.PageNumber);
        Assert.Equal(query.PageSize, page.PageSize);
    }

    [Fact]
    public void GetProducts_InvalidQueryDoesNotReadStorage()
    {
        var repository = new FindOnlyRepository([]);
        var service = CreateService(repository);

        Assert.Throws<ValidationException>(() => service.GetProducts(new() { PageSize = 0 }));
        Assert.Equal(0, repository.FindCount);
    }

    [Fact]
    public void GetProducts_UsesRepositorySearchInsteadOfReadingFullCatalog()
    {
        var product = CreateCatalog()[0];
        var repository = new FindOnlyRepository([product]);
        var service = CreateService(repository);

        var page = service.GetProducts(new());

        Assert.Equal(product, Assert.Single(page.Items));
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(1, repository.FindCount);
    }

    private static Product[] CreateCatalog()
    {
        var products = Enumerable.Range(1, 1500)
            .Select(index => new Product(
                Id: Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
                Name: $"Товар {index}",
                Price: index,
                Weight: 1,
                Type: (ProductType)(index % 4),
                CreatedAt: Start.AddDays(index % 10),
                WarehouseNumber: index % 3 + 1))
            .ToArray();

        // Input order differs from both creation time and Id order.
        new Random(42).Shuffle(products);
        return products;
    }

    private static ProductService CreateService(IProductRepository repository) =>
        new(repository, new CreateProductValidator(), new ProductQueryValidator(), TimeProvider.System);

    private sealed class FindOnlyRepository(IReadOnlyList<Product> products) : IProductRepository
    {
        public int FindCount { get; private set; }

        public IReadOnlyList<Product> Find(ProductQuery query)
        {
            FindCount++;
            return products;
        }

        public void Add(Product product) => throw new NotSupportedException();
        public Product? GetById(Guid id) => throw new NotSupportedException();

        public IEnumerable<Product> GetAll() => throw new NotSupportedException("Полный обход каталога запрещён.");
    }
}
