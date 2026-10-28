using Products.Api.Domain;

namespace Products.Api.Application;

public interface IProductRepository
{
    void Add(Product product);
    Product? GetById(Guid id);
    IEnumerable<Product> GetAll();

    // Returns a stable collection of matches before sorting and pagination.
    IReadOnlyList<Product> Find(ProductQuery query);
}
