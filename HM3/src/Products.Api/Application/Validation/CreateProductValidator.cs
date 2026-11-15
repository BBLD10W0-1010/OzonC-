using FluentValidation;
using Products.Api.Domain;

namespace Products.Api.Application.Validation;

public sealed class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.MaxNameLength);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Weight)
            .Must(weight => double.IsFinite(weight) && weight > 0)
            .WithMessage("Вес должен быть конечным положительным числом.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.WarehouseNumber).GreaterThan(0);
    }
}
