using FluentValidation;

namespace Products.Api.Application.Validation;

public sealed class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    public ProductQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ProductQuery.MaxPageSize);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.WarehouseNumber).GreaterThan(0).When(x => x.WarehouseNumber.HasValue);
        RuleFor(x => x.CreatedTo)
            .Must(HasValidDateRange)
            .WithMessage("Конец периода не может быть раньше начала.");
    }

    private static bool HasValidDateRange(ProductQuery query, DateTimeOffset? createdTo) =>
        query.CreatedFrom is not { } createdFrom ||
        createdTo is not { } end ||
        createdFrom <= end;
}
