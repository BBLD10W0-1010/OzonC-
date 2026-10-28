using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Products.Api.Application;

namespace Products.Api.OpenApi;

internal static class GrpcProductOperations
{
    public static OpenApiPathItem CreateCollectionPath() => new()
    {
        Operations = new Dictionary<OperationType, OpenApiOperation>
        {
            [OperationType.Post] = CreateProduct(),
            [OperationType.Get] = ListProducts()
        }
    };

    public static OpenApiPathItem CreateProductPath() => new()
    {
        Operations = new Dictionary<OperationType, OpenApiOperation>
        {
            [OperationType.Get] = GetProduct()
        }
    };

    private static OpenApiOperation CreateProduct()
    {
        var operation = Operation("GrpcCreateProduct", "Создать товар через gRPC JSON", GrpcProductSchemas.CreatedProduct);
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = JsonContent(GrpcProductSchemas.CreateRequest)
        };
        return operation;
    }

    private static OpenApiOperation ListProducts()
    {
        var operation = Operation("GrpcGetProducts", "Список товаров через gRPC JSON", GrpcProductSchemas.ProductPage);
        operation.Parameters = new List<OpenApiParameter>
        {
            Query("createdFrom", new() { Type = "string", Format = "date-time" }, "Начало периода включительно (RFC 3339)."),
            Query("createdTo", new() { Type = "string", Format = "date-time" }, "Конец периода включительно (RFC 3339)."),
            Query("type", GrpcProductSchemas.ProductType(), "Вид товара; GENERAL — полноценный фильтр."),
            Query("warehouseNumber", new() { Type = "integer", Minimum = 1 }, "Номер склада."),
            Query("pageNumber", new()
            {
                Type = "integer", Minimum = 1, Default = new OpenApiInteger(ProductQuery.DefaultPageNumber)
            }, "Страница, начиная с 1."),
            Query("pageSize", new()
            {
                Type = "integer", Minimum = 1, Maximum = ProductQuery.MaxPageSize,
                Default = new OpenApiInteger(ProductQuery.DefaultPageSize)
            }, "Размер страницы.")
        };
        return operation;
    }

    private static OpenApiOperation GetProduct()
    {
        var operation = Operation("GrpcGetProduct", "Получить товар через gRPC JSON", GrpcProductSchemas.Product);
        operation.Parameters = new List<OpenApiParameter>
        {
            new()
            {
                Name = "id", In = ParameterLocation.Path, Required = true,
                Schema = new() { Type = "string", Format = "uuid" }
            }
        };
        operation.Responses["404"] = ErrorResponse("Товар не найден.");
        return operation;
    }

    private static OpenApiOperation Operation(string id, string summary, string response) => new()
    {
        OperationId = id,
        Summary = summary,
        Tags = new List<OpenApiTag> { new() { Name = "gRPC (JSON transcoding)" } },
        Responses = new OpenApiResponses
        {
            ["200"] = new() { Description = "Успешно.", Content = JsonContent(response) },
            ["400"] = ErrorResponse("Некорректный запрос."),
            ["500"] = ErrorResponse("Ошибка сервера.")
        }
    };

    private static OpenApiParameter Query(string name, OpenApiSchema schema, string description) =>
        new() { Name = name, In = ParameterLocation.Query, Schema = schema, Description = description };

    private static OpenApiResponse ErrorResponse(string description) =>
        new() { Description = description, Content = JsonContent(GrpcProductSchemas.Error) };

    private static Dictionary<string, OpenApiMediaType> JsonContent(string schemaName) =>
        new() { ["application/json"] = new() { Schema = GrpcProductSchemas.Reference(schemaName) } };
}
