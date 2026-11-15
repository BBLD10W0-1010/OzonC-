using Products.Api.Domain;

namespace Products.Api.Infrastructure;

internal sealed record ProductRow(
    Guid Id,
    string Name,
    decimal Price,
    double Weight,
    int Type,
    DateTime CreatedAt,
    int WarehouseNumber)
{
    public Product ToProduct() => new(Id, Name, Price, Weight, (ProductType)Type, CreatedAt, WarehouseNumber);
}
