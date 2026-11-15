using Products.Api.Domain;

namespace Products.Api.Application;

public interface IProductRepository
{
    Task<Guid> AddAsync(Product product, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> FindAsync(
        ProductQuery query, CancellationToken cancellationToken);
}
