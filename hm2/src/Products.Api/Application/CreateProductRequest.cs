using Products.Api.Domain;

namespace Products.Api.Application;

public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    double Weight,
    ProductType Type,
    int WarehouseNumber);
