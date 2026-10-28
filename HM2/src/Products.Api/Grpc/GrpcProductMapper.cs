using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Google.Protobuf.WellKnownTypes;
using Products.Api.Application;
using GrpcContracts = Products.Contracts.Grpc;

namespace Products.Api.Grpc;

public static class GrpcProductMapper
{
    public static CreateProductRequest ToCreateRequest(GrpcContracts.CreateProductRequest request) => new(
        Name: request.Name,
        Price: ParsePrice(request.Price),
        Weight: request.Weight,
        Type: (Domain.ProductType)request.Type,
        WarehouseNumber: request.WarehouseNumber);

    public static ProductQuery ToQuery(GrpcContracts.GetProductsRequest request) => new()
    {
        CreatedFrom = ParseDate(request.CreatedFrom, nameof(ProductQuery.CreatedFrom)),
        CreatedTo = ParseDate(request.CreatedTo, nameof(ProductQuery.CreatedTo)),
        Type = request.HasType ? (Domain.ProductType)request.Type : null,
        WarehouseNumber = request.HasWarehouseNumber ? request.WarehouseNumber : null,
        PageNumber = request.HasPageNumber ? request.PageNumber : ProductQuery.DefaultPageNumber,
        PageSize = request.HasPageSize ? request.PageSize : ProductQuery.DefaultPageSize
    };

    public static Guid ParseId(string id)
    {
        if (Guid.TryParse(id, out var productId))
        {
            return productId;
        }

        throw InvalidField(nameof(Domain.Product.Id), "Id должен быть корректным UUID.");
    }

    public static GrpcContracts.GetProductsResponse ToGrpc(PagedProducts page)
    {
        var response = new GrpcContracts.GetProductsResponse
        {
            TotalCount = page.TotalCount,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalPages = page.TotalPages
        };

        response.Items.AddRange(page.Items.Select(ToGrpc));
        return response;
    }

    public static GrpcContracts.Product ToGrpc(Domain.Product product) => new()
    {
        Id = product.Id.ToString(),
        Name = product.Name,
        Price = product.Price.ToString(CultureInfo.InvariantCulture),
        Weight = product.Weight,
        Type = (GrpcContracts.ProductType)product.Type,
        CreatedAt = Timestamp.FromDateTimeOffset(product.CreatedAt),
        WarehouseNumber = product.WarehouseNumber
    };

    private static decimal ParsePrice(string value)
    {
        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var price))
        {
            throw InvalidField(nameof(CreateProductRequest.Price),
                "Цена должна быть десятичным числом, например 199.90.");
        }

        return price;
    }

    private static DateTimeOffset? ParseDate(Timestamp? value, string field)
    {
        try
        {
            return value?.ToDateTimeOffset();
        }
        catch (InvalidOperationException)
        {
            throw InvalidField(field, "Некорректная дата Protobuf Timestamp.");
        }
    }

    private static ValidationException InvalidField(string field, string message) =>
        new([new ValidationFailure(field, message)]);
}
