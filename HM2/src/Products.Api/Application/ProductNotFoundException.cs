namespace Products.Api.Application;

public sealed class ProductNotFoundException(Guid id)
    : Exception($"Товар с Id '{id}' не найден.");
