using Products.Api.Domain;

namespace Products.Api.Application.Queries;

internal static class ProductFilter
{
    public static bool Matches(Product product, ProductQuery query) =>
        (!query.Type.HasValue || product.Type == query.Type.Value) &&
        (!query.WarehouseNumber.HasValue || product.WarehouseNumber == query.WarehouseNumber.Value) &&
        (!query.CreatedFrom.HasValue || product.CreatedAt >= query.CreatedFrom.Value) &&
        (!query.CreatedTo.HasValue || product.CreatedAt <= query.CreatedTo.Value);
}
