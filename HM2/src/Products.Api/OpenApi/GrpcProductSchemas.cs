using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using DomainProduct = Products.Api.Domain.Product;
using GrpcProduct = Products.Contracts.Grpc.Product;

namespace Products.Api.OpenApi;

internal static class GrpcProductSchemas
{
    public const string CreateRequest = "GrpcCreateProduct";
    public const string CreatedProduct = "GrpcCreatedProduct";
    public const string Product = "GrpcProduct";
    public const string ProductPage = "GrpcProductPage";
    public const string Error = "GrpcError";

    public static void Register(IDictionary<string, OpenApiSchema> schemas)
    {
        schemas[CreateRequest] = CreateRequestSchema();
        schemas[CreatedProduct] = Object(new() { ["id"] = Uuid() });
        schemas[Product] = CreateProductSchema();
        schemas[ProductPage] = CreatePageSchema();
        schemas[Error] = CreateErrorSchema();
    }

    public static OpenApiSchema Reference(string name) => new()
    {
        Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = name }
    };

    public static OpenApiSchema ProductType() => new()
    {
        Type = "string",
        Enum = GrpcProduct.Descriptor.FindFieldByNumber(GrpcProduct.TypeFieldNumber).EnumType.Values
            .Select(value => (IOpenApiAny)new OpenApiString(value.Name))
            .ToList()
    };

    private static OpenApiSchema CreateRequestSchema()
    {
        var schema = Object(CreateProductProperties());
        schema.Required = new HashSet<string> { "name", "price", "weight", "warehouseNumber" };
        return schema;
    }

    private static OpenApiSchema CreateProductSchema()
    {
        var properties = CreateProductProperties();
        properties["id"] = Uuid();
        properties["createdAt"] = new() { Type = "string", Format = "date-time" };
        return Object(properties);
    }

    private static Dictionary<string, OpenApiSchema> CreateProductProperties() => new()
    {
        ["name"] = new()
        {
            Type = "string",
            MaxLength = DomainProduct.MaxNameLength,
            Example = new OpenApiString("Молоко")
        },
        ["price"] = new()
        {
            Type = "string",
            Example = new OpenApiString("99.90"),
            Description = "Decimal с точкой; положительное число."
        },
        ["weight"] = new()
        {
            Type = "number",
            Format = "double",
            Minimum = 0,
            ExclusiveMinimum = true,
            Example = new OpenApiDouble(1)
        },
        ["type"] = ProductType(),
        ["warehouseNumber"] = new()
        {
            Type = "integer",
            Format = "int32",
            Minimum = 1,
            Example = new OpenApiInteger(1)
        }
    };

    private static OpenApiSchema CreatePageSchema() => Object(new()
    {
        ["items"] = new() { Type = "array", Items = Reference(Product) },
        ["totalCount"] = Integer(),
        ["pageNumber"] = Integer(),
        ["pageSize"] = Integer(),
        ["totalPages"] = Integer()
    });

    private static OpenApiSchema CreateErrorSchema() => Object(new()
    {
        ["code"] = Integer(),
        ["message"] = new() { Type = "string" },
        ["details"] = new() { Type = "array", Items = new() { Type = "object" } }
    });

    private static OpenApiSchema Object(Dictionary<string, OpenApiSchema> properties) =>
        new() { Type = "object", Properties = properties };

    private static OpenApiSchema Integer() => new() { Type = "integer", Format = "int32" };
    private static OpenApiSchema Uuid() => new() { Type = "string", Format = "uuid" };
}
