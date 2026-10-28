using Products.Api.Domain;

namespace Products.Api.Infrastructure;

// The repository protects every access to this index with its shared lock.
internal sealed class ProductCreationIndex
{
    private readonly SortedList<(long UtcTicks, Guid ProductId), Product> _products = new();

    public void Add(Product product) =>
        _products.Add((product.CreatedAt.UtcTicks, product.Id), product);

    public (int Start, int Count) FindRange(DateTimeOffset? createdFrom, DateTimeOffset? createdTo)
    {
        var start = createdFrom.HasValue
            ? FindBoundary(createdFrom.Value.UtcTicks, skipEqualDates: false)
            : 0;
        var end = createdTo.HasValue
            ? FindBoundary(createdTo.Value.UtcTicks, skipEqualDates: true)
            : _products.Count;

        return (start, Math.Max(0, end - start));
    }

    public IEnumerable<Product> GetProducts((int Start, int Count) range)
    {
        var end = range.Start + range.Count;
        for (var index = range.Start; index < end; index++)
        {
            yield return _products.Values[index];
        }
    }

    private int FindBoundary(long utcTicks, bool skipEqualDates)
    {
        var lower = 0;
        var upper = _products.Count;

        while (lower < upper)
        {
            var middle = lower + (upper - lower) / 2;
            var currentTicks = _products.Keys[middle].UtcTicks;
            if (currentTicks < utcTicks || (skipEqualDates && currentTicks == utcTicks))
            {
                lower = middle + 1;
            }
            else
            {
                upper = middle;
            }
        }

        return lower;
    }
}
