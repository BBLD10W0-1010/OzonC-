using Products.Api.Domain;

namespace Products.Api.Application;

public sealed record PagedProducts(
    IReadOnlyList<Product> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);
