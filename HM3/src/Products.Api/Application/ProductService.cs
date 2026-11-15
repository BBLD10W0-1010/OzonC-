using FluentValidation;
using Products.Api.Domain;

namespace Products.Api.Application;

public sealed class ProductService(
    IProductRepository repository,
    IValidator<ProductDetails> detailsValidator,
    IValidator<ProductQuery> queryValidator,
    TimeProvider timeProvider)
{
    public Task<Guid> CreateAsync(ProductDetails details, CancellationToken cancellationToken)
    {
        detailsValidator.ValidateAndThrow(details);

        var product = new Product(
            Id: Guid.NewGuid(),
            Name: details.Name.Trim(),
            Price: details.Price,
            Weight: details.Weight,
            Type: details.Type,
            CreatedAt: timeProvider.GetUtcNow(),
            WarehouseNumber: details.WarehouseNumber);

        return repository.AddAsync(product, cancellationToken);
    }

    public async Task<Product> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) ?? throw new ProductNotFoundException(id);

    public async Task<Product> UpdateAsync(Guid id, ProductDetails details, CancellationToken cancellationToken)
    {
        detailsValidator.ValidateAndThrow(details);
        return await repository.UpdateAsync(id, details with { Name = details.Name.Trim() }, cancellationToken)
            ?? throw new ProductNotFoundException(id);
    }

    public async Task<PagedProducts> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken)
    {
        queryValidator.ValidateAndThrow(query);

        var (items, totalCount) = await repository.FindAsync(query, cancellationToken);
        var totalPages = (int)(((long)totalCount + query.PageSize - 1) / query.PageSize);
        return new PagedProducts(items, totalCount, query.PageNumber, query.PageSize, totalPages);
    }
}
