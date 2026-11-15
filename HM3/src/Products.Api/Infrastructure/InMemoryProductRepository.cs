using System.Collections.Concurrent;
using Products.Api.Application;
using Products.Api.Application.Queries;
using Products.Api.Domain;

namespace Products.Api.Infrastructure;

public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();
    private readonly Dictionary<int, HashSet<Guid>> _productIdsByWarehouse = new();
    private readonly Dictionary<ProductType, HashSet<Guid>> _productIdsByType = new();
    private readonly ProductCreationIndex _creationIndex = new();
    private readonly Lock _indexLock = new();

    public void Add(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        lock (_indexLock)
        {
            if (!_products.TryAdd(product.Id, product))
            {
                throw new InvalidOperationException("Товар с таким Id уже существует.");
            }

            AddToIndex(_productIdsByWarehouse, product.WarehouseNumber, product.Id);
            AddToIndex(_productIdsByType, product.Type, product.Id);
            _creationIndex.Add(product);
        }
    }

    public Product? GetById(Guid id) =>
        _products.TryGetValue(id, out var product) ? product : null;

    public IEnumerable<Product> GetAll() => _products.Select(pair => pair.Value);

    public IReadOnlyList<Product> Find(ProductQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (_indexLock)
        {
            // Materialize matches before releasing the lock; sorting runs in the application layer.
            return SelectCandidates(query)
                .Where(product => ProductFilter.Matches(product, query))
                .ToArray();
        }
    }

    // Called only under _indexLock: index enumerators never escape the repository.
    private IEnumerable<Product> SelectCandidates(ProductQuery query)
    {
        HashSet<Guid>? smallestGroup = null;

        if (query.WarehouseNumber is int warehouseNumber)
        {
            if (!_productIdsByWarehouse.TryGetValue(warehouseNumber, out smallestGroup))
            {
                return [];
            }
        }

        if (query.Type is ProductType type)
        {
            if (!_productIdsByType.TryGetValue(type, out var typeGroup))
            {
                return [];
            }

            if (smallestGroup is null || typeGroup.Count < smallestGroup.Count)
            {
                smallestGroup = typeGroup;
            }
        }

        if (query.CreatedFrom.HasValue || query.CreatedTo.HasValue)
        {
            var dateRange = _creationIndex.FindRange(query.CreatedFrom, query.CreatedTo);
            if (dateRange.Count == 0)
            {
                return [];
            }

            if (smallestGroup is null || dateRange.Count < smallestGroup.Count)
            {
                return _creationIndex.GetProducts(dateRange);
            }
        }

        return smallestGroup is null
            ? GetAll()
            : smallestGroup.Select(id => _products[id]);
    }

    private static void AddToIndex<TKey>(Dictionary<TKey, HashSet<Guid>> index, TKey key, Guid id)
        where TKey : notnull
    {
        if (!index.TryGetValue(key, out var productIds))
        {
            productIds = new HashSet<Guid>();
            index.Add(key, productIds);
        }

        productIds.Add(id);
    }
}
