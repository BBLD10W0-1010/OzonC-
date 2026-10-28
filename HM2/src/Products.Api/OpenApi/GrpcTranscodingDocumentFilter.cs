using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.Api.OpenApi;

// Swagger describes the HTTP bindings of the real gRPC service.
public sealed class GrpcTranscodingDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        GrpcProductSchemas.Register(document.Components.Schemas);
        document.Paths["/v1/products"] = GrpcProductOperations.CreateCollectionPath();
        document.Paths["/v1/products/{id}"] = GrpcProductOperations.CreateProductPath();
    }
}
