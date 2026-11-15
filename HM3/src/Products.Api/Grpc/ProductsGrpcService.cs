using Grpc.Core;
using Products.Api.Application;
using GrpcContracts = Products.Contracts.Grpc;

namespace Products.Api.Grpc;

public sealed class ProductsGrpcService(ProductService service) : GrpcContracts.ProductsService.ProductsServiceBase
{
    public override async Task<GrpcContracts.CreateProductResponse> CreateProduct(
        GrpcContracts.CreateProductRequest request, ServerCallContext context)
    {
        var createRequest = GrpcProductMapper.ToDetails(request);
        var id = await service.CreateAsync(createRequest, context.CancellationToken);

        return new GrpcContracts.CreateProductResponse { Id = id.ToString() };
    }

    public override async Task<GrpcContracts.GetProductsResponse> GetProducts(
        GrpcContracts.GetProductsRequest request, ServerCallContext context)
    {
        var query = GrpcProductMapper.ToQuery(request);
        var page = await service.GetProductsAsync(query, context.CancellationToken);

        return GrpcProductMapper.ToGrpc(page);
    }

    public override async Task<GrpcContracts.Product> GetProduct(
        GrpcContracts.GetProductRequest request, ServerCallContext context)
    {
        var id = GrpcProductMapper.ParseId(request.Id);
        var product = await service.GetByIdAsync(id, context.CancellationToken);

        return GrpcProductMapper.ToGrpc(product);
    }

    public override async Task<GrpcContracts.Product> UpdateProduct(
        GrpcContracts.UpdateProductRequest request, ServerCallContext context)
    {
        var id = GrpcProductMapper.ParseId(request.Id);
        var details = GrpcProductMapper.ToDetails(request);
        var product = await service.UpdateAsync(id, details, context.CancellationToken);
        return GrpcProductMapper.ToGrpc(product);
    }
}
