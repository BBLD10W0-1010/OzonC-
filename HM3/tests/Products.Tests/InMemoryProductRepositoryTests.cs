using Products.Api.Application;
using Products.Api.Domain;
using Products.Api.Infrastructure;

namespace Products.Tests;

public sealed class InMemoryProductRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<ProductQuery> Queries => new()
    {
        new(),
        new() { WarehouseNumber = 1 },
        new() { WarehouseNumber = 99 },
        new() { WarehouseNumber = 999 },
        new() { Type = ProductType.General },
        new() { Type = ProductType.HouseholdChemicals },
        new() { Type = ProductType.Food },
        new() { CreatedFrom = Start.AddTicks(100) },
        new() { CreatedTo = Start.AddTicks(200) },
        new() { CreatedFrom = Start.AddTicks(100), CreatedTo = Start.AddTicks(200) },
        new() { CreatedFrom = Start.AddTicks(200), CreatedTo = Start.AddTicks(200) },
        new() { CreatedFrom = Start.AddDays(1) },
        new() { CreatedTo = Start.AddDays(-1) },
        new() { CreatedFrom = DateTimeOffset.MinValue, CreatedTo = DateTimeOffset.MaxValue },
        new() { Type = ProductType.Food, WarehouseNumber = 1 },
        new() { Type = ProductType.Food, WarehouseNumber = 99 },
        new() { WarehouseNumber = 1, CreatedFrom = Start.AddTicks(100), CreatedTo = Start.AddTicks(900) },
        new() { Type = ProductType.General, CreatedFrom = Start.AddTicks(100), CreatedTo = Start.AddTicks(200) },
        new()
        {
            Type = ProductType.Food,
            WarehouseNumber = 99,
            CreatedFrom = Start.AddTicks(100),
            CreatedTo = Start.AddTicks(200)
        },
        new()
        {
            Type = ProductType.Food,
            WarehouseNumber = 1,
            CreatedFrom = Start.AddTicks(200).ToOffset(TimeSpan.FromHours(5)),
            CreatedTo = Start.AddTicks(900).ToOffset(TimeSpan.FromHours(-3))
        }
    };

    [Theory]
    [MemberData(nameof(Queries))]
    public void Find_AllFilterCombinationsMatchFullScan(ProductQuery query)
    {
        var products = CreateCatalog();
        var repository = CreateRepository(products);

        var expected = products.Where(product => MatchesReference(product, query)).Select(product => product.Id).Order();
        var actual = repository.Find(query).Select(product => product.Id).Order();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Find_DateRangeIncludesAllProductsAtEqualUtcInstant()
    {
        var products = new[]
        {
            CreateProduct(1, Start),
            CreateProduct(2, Start.ToOffset(TimeSpan.FromHours(5))),
            CreateProduct(3, Start.ToOffset(TimeSpan.FromHours(-7))),
            CreateProduct(4, Start.AddTicks(-1)),
            CreateProduct(5, Start.AddTicks(1))
        };
        var repository = CreateRepository(products);

        var matches = repository.Find(new() { CreatedFrom = Start, CreatedTo = Start });

        Assert.Equal(products.Take(3).Select(product => product.Id).Order(), matches.Select(product => product.Id).Order());
    }

    [Fact]
    public void Find_DateIndexHandlesMinimumAndMaximumDates()
    {
        var oldest = CreateProduct(1, DateTimeOffset.MinValue);
        var newest = CreateProduct(2, DateTimeOffset.MaxValue);
        var repository = CreateRepository([newest, oldest]);

        Assert.Equal(oldest, Assert.Single(repository.Find(new() { CreatedTo = DateTimeOffset.MinValue })));
        Assert.Equal(newest, Assert.Single(repository.Find(new() { CreatedFrom = DateTimeOffset.MaxValue })));
        Assert.Equal(2, repository.Find(new()
        {
            CreatedFrom = DateTimeOffset.MinValue,
            CreatedTo = DateTimeOffset.MaxValue
        }).Count);
    }

    [Fact]
    public void Find_DateIndexRetainsDifferentProductsWithIdenticalTimestamps()
    {
        var products = Enumerable.Range(1, 200).Select(index => CreateProduct(index, Start)).ToArray();
        var repository = CreateRepository(products.Reverse());

        var matches = repository.Find(new() { CreatedFrom = Start, CreatedTo = Start });

        Assert.Equal(products.Select(product => product.Id).Order(), matches.Select(product => product.Id).Order());
    }

    [Fact]
    public void Find_ResultIsStableAfterLaterWrites()
    {
        var first = CreateProduct(1, Start);
        var second = CreateProduct(2, Start);
        var repository = CreateRepository([first]);
        var query = new ProductQuery { Type = first.Type, WarehouseNumber = first.WarehouseNumber, CreatedTo = Start };

        var previousResult = repository.Find(query);
        repository.Add(second);

        Assert.Equal(first, Assert.Single(previousResult));
        Assert.Equal(2, repository.Find(query).Count);
    }

    [Fact]
    public void Add_DuplicateIdDoesNotPolluteAnyIndex()
    {
        var original = CreateProduct(1, Start);
        var duplicate = original with
        {
            Type = ProductType.Electronics,
            WarehouseNumber = 50,
            CreatedAt = Start.AddDays(10)
        };
        var repository = CreateRepository([original]);

        Assert.Throws<InvalidOperationException>(() => repository.Add(duplicate));

        Assert.Equal(original, repository.GetById(original.Id));
        Assert.Equal(original, Assert.Single(repository.Find(new())));
        Assert.Empty(repository.Find(new() { Type = duplicate.Type }));
        Assert.Empty(repository.Find(new() { WarehouseNumber = duplicate.WarehouseNumber }));
        Assert.Empty(repository.Find(new() { CreatedFrom = duplicate.CreatedAt, CreatedTo = duplicate.CreatedAt }));
    }

    [Fact]
    public void Add_ParallelReadersSeeAllIndexesAfterEachSuccessfulWrite()
    {
        var products = CreateCatalog();
        var repository = new InMemoryProductRepository();

        Parallel.ForEach(products, new ParallelOptions { MaxDegreeOfParallelism = 8 }, product =>
        {
            repository.Add(product);

            var matches = repository.Find(new()
            {
                Type = product.Type,
                WarehouseNumber = product.WarehouseNumber,
                CreatedFrom = product.CreatedAt,
                CreatedTo = product.CreatedAt
            });

            Assert.Contains(product, matches);
            Assert.Equal(product, repository.GetById(product.Id));
        });

        Assert.Equal(products.Length, repository.Find(new()).Count);
        foreach (var query in Queries)
        {
            var expected = products.Where(product => MatchesReference(product, query)).Select(product => product.Id).Order();
            var actual = repository.Find(query).Select(product => product.Id).Order();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Add_ConcurrentDuplicateIdsCreateOnlyOneIndexedProduct()
    {
        var variants = Enumerable.Range(1, 32)
            .Select(index => CreateProduct(1, Start.AddTicks(index)) with
            {
                Type = (ProductType)(index % 4),
                WarehouseNumber = index
            })
            .ToArray();
        var repository = new InMemoryProductRepository();
        var successes = 0;

        Parallel.ForEach(variants, product =>
        {
            try
            {
                repository.Add(product);
                Interlocked.Increment(ref successes);
            }
            catch (InvalidOperationException)
            {
                // Another request already added this Id.
            }
        });

        Assert.Equal(1, successes);
        var stored = Assert.Single(repository.Find(new()));
        foreach (var variant in variants)
        {
            var matches = repository.Find(new()
            {
                WarehouseNumber = variant.WarehouseNumber,
                Type = variant.Type,
                CreatedFrom = variant.CreatedAt,
                CreatedTo = variant.CreatedAt
            });
            Assert.Equal(variant == stored ? 1 : 0, matches.Count);
        }
    }

    [Fact]
    public void GetAll_RemainsLazyWithoutCopyingCatalog()
    {
        var repository = new InMemoryProductRepository();
        var products = repository.GetAll();
        var product = CreateProduct(1, Start);

        repository.Add(product);

        Assert.Equal(product, Assert.Single(products));
    }

    private static bool MatchesReference(Product product, ProductQuery query)
    {
        if (query.WarehouseNumber.HasValue && product.WarehouseNumber != query.WarehouseNumber) return false;
        if (query.Type.HasValue && product.Type != query.Type) return false;
        if (query.CreatedFrom.HasValue && product.CreatedAt < query.CreatedFrom) return false;
        if (query.CreatedTo.HasValue && product.CreatedAt > query.CreatedTo) return false;
        return true;
    }

    private static Product[] CreateCatalog()
    {
        var products = Enumerable.Range(1, 1000)
            .Select(index => CreateProduct(index, Start.AddTicks(index % 250)) with
            {
                Type = (ProductType)(index % 4),
                WarehouseNumber = index % 2 == 0 ? 99 : index % 7 + 1
            })
            .ToArray();
        new Random(42).Shuffle(products);
        return products;
    }

    private static Product CreateProduct(int index, DateTimeOffset createdAt) => new(
        Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
        $"Товар {index}", 1m, 1, ProductType.General, createdAt, 1);

    private static InMemoryProductRepository CreateRepository(IEnumerable<Product> products)
    {
        var repository = new InMemoryProductRepository();
        foreach (var product in products)
        {
            repository.Add(product);
        }
        return repository;
    }
}
