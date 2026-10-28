namespace Products.Api.Domain;

public sealed record Product(
    Guid Id,
    string Name,
    decimal Price,
    double Weight,
    ProductType Type,
    DateTimeOffset CreatedAt,
    int WarehouseNumber)
{
    public const int MaxNameLength = 200;
}
