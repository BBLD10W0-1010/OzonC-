using FluentValidation;
using Products.Api.Domain;

namespace Products.Api.Application;

public sealed class ProductService(
    IProductRepository repository,
    IValidator<CreateProductRequest> createValidator,
    IValidator<ProductQuery> queryValidator,
    TimeProvider timeProvider)
{
    public Task<Guid> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
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

        return repository.AddAsync(product, cancellationToken);
    }

    public async Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new ProductNotFoundException(id);

    public async Task<PagedProducts> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        queryValidator.ValidateAndThrow(query);

        var (items, totalCount) = await repository.FindAsync(query, cancellationToken);
        var totalPages = (int)(((long)totalCount + query.PageSize - 1) / query.PageSize);
        return new PagedProducts(items, totalCount, query.PageNumber, query.PageSize, totalPages);
    }
}
