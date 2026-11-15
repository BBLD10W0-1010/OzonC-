using Products.Api.Domain;

namespace Products.Api.Application;

public sealed record ProductDetails(
    string Name,
    decimal Price,
    double Weight,
    ProductType Type,
    int WarehouseNumber);
