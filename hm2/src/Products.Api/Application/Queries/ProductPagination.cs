using Products.Api.Domain;

namespace Products.Api.Application.Queries;

internal static class ProductPagination
{
    public static PagedProducts CreatePage(IReadOnlyList<Product> matches, ProductQuery query)
    {
        var totalCount = matches.Count;
        var totalPages = (int)(((long)totalCount + query.PageSize - 1) / query.PageSize);
        var offset = ((long)query.PageNumber - 1) * query.PageSize;

        if (offset >= totalCount)
        {
            return new PagedProducts([], totalCount, query.PageNumber, query.PageSize, totalPages);
        }

        var items = SelectPage(matches, (int)offset, query.PageSize);
        return new PagedProducts(items, totalCount, query.PageNumber, query.PageSize, totalPages);
    }

    private static Product[] SelectPage(IEnumerable<Product> matches, int offset, int pageSize) =>
        matches
            .OrderBy(product => product.CreatedAt)
            .ThenBy(product => product.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToArray();
}
