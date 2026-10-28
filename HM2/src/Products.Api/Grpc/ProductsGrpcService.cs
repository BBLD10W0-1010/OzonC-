using Grpc.Core;
using Products.Api.Application;
using GrpcContracts = Products.Contracts.Grpc;

namespace Products.Api.Grpc;

public sealed class ProductsGrpcService(ProductService service) : GrpcContracts.ProductsService.ProductsServiceBase
{
    public override Task<GrpcContracts.CreateProductResponse> CreateProduct(
        GrpcContracts.CreateProductRequest request, ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var createRequest = GrpcProductMapper.ToCreateRequest(request);
        var id = service.Create(createRequest);

        return Task.FromResult(new GrpcContracts.CreateProductResponse { Id = id.ToString() });
    }

    public override Task<GrpcContracts.GetProductsResponse> GetProducts(
        GrpcContracts.GetProductsRequest request, ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var query = GrpcProductMapper.ToQuery(request);
        var page = service.GetProducts(query);

        return Task.FromResult(GrpcProductMapper.ToGrpc(page));
    }

    public override Task<GrpcContracts.Product> GetProduct(
        GrpcContracts.GetProductRequest request, ServerCallContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var id = GrpcProductMapper.ParseId(request.Id);
        var product = service.GetById(id);

        return Task.FromResult(GrpcProductMapper.ToGrpc(product));
    }
}
