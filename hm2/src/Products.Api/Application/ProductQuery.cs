using Products.Api.Domain;

namespace Products.Api.Application;

public sealed class ProductQuery
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public ProductType? Type { get; init; }
    public int? WarehouseNumber { get; init; }
    public int PageNumber { get; init; } = DefaultPageNumber;
    public int PageSize { get; init; } = DefaultPageSize;
}
