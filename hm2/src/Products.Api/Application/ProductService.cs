using FluentValidation;
using Products.Api.Application.Queries;
using Products.Api.Domain;

namespace Products.Api.Application;

public sealed class ProductService(
    IProductRepository repository,
    IValidator<CreateProductRequest> createValidator,
    IValidator<ProductQuery> queryValidator,
    TimeProvider timeProvider)
{
    public Guid Create(CreateProductRequest request)
    {
        createValidator.ValidateAndThrow(request);

        var product = new Product(
            Id: Guid.NewGuid(),
            Name: request.Name.Trim(),
            Price: request.Price,
            Weight: request.Weight,
            Type: request.Type,
            CreatedAt: timeProvider.GetUtcNow(),
            WarehouseNumber: request.WarehouseNumber);

        repository.Add(product);
        return product.Id;
    }

    public Product GetById(Guid id) =>
        repository.GetById(id) ?? throw new ProductNotFoundException(id);

    public PagedProducts GetProducts(ProductQuery query)
    {
        queryValidator.ValidateAndThrow(query);

        var matches = repository.Find(query);
        return ProductPagination.CreatePage(matches, query);
    }
}
