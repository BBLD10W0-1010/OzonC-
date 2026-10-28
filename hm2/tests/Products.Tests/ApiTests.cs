using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Products.Api.Application;
using Products.Api.Domain;
using GrpcContracts = Products.Contracts.Grpc;

namespace Products.Tests;

public sealed class ApiTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _http;

    public ApiTests() => _http = _factory.CreateClient();

    [Fact]
    public async Task Http_CreatesReadsAndListsProduct()
    {
        using var created = await _http.PostAsJsonAsync("/api/products", ValidHttpProduct());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<CreateProductResponse>())!.Id;
        Assert.EndsWith($"/api/products/{id}", created.Headers.Location!.ToString());

        var product = await _http.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
        Assert.Equal("Молоко", product.GetProperty("name").GetString());
        Assert.Equal(99.90m, product.GetProperty("price").GetDecimal());
        Assert.Equal("Food", product.GetProperty("type").GetString());
        Assert.True(product.GetProperty("createdAt").GetDateTimeOffset() <= DateTimeOffset.UtcNow);

        var page = await _http.GetFromJsonAsync<JsonElement>("/api/products?type=Food&warehouseNumber=2&pageSize=1&pageNumber=1");
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(id, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData("/api/products?pageNumber=0")]
    [InlineData("/api/products?pageSize=101")]
    [InlineData("/api/products?type=99")]
    [InlineData("/api/products?type=unknown")]
    [InlineData("/api/products?warehouseNumber=-1")]
    [InlineData("/api/products?createdFrom=2026-10-06T00:00:00Z&createdTo=2026-10-05T00:00:00Z")]
    [InlineData("/api/products?createdFrom=not-a-date")]
    [InlineData("/api/products/not-a-guid")]
    public async Task Http_InvalidQueryOrIdReturns400(string path)
    {
        using var response = await _http.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Http_InvalidCreateReturnsValidationDetailsAndDoesNotStoreProduct()
    {
        using var response = await _http.PostAsJsonAsync("/api/products", new
        {
            name = " ",
            price = -10,
            weight = 0,
            type = "Food",
            warehouseNumber = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").TryGetProperty("Price", out _));
        var page = await _http.GetFromJsonAsync<JsonElement>("/api/products");
        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Http_MalformedOrNullBodyReturns400(string body)
    {
        using var response = await _http.PostAsync("/api/products", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Http_MissingProductReturns404()
    {
        using var response = await _http.GetAsync($"/api/products/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Grpc_AndHttpShareProductsAndFilters()
    {
        using var channel = CreateChannel(_factory);
        var grpc = new GrpcContracts.ProductsService.ProductsServiceClient(channel);
        var created = await grpc.CreateProductAsync(ValidGrpcProduct());
        var product = await grpc.GetProductAsync(new() { Id = created.Id });
        Assert.Equal("99.90", product.Price);
        Assert.NotNull(product.CreatedAt);

        var page = await grpc.GetProductsAsync(new()
        {
            Type = GrpcContracts.ProductType.Food,
            WarehouseNumber = 2,
            CreatedFrom = product.CreatedAt,
            CreatedTo = product.CreatedAt,
            PageNumber = 1,
            PageSize = 1
        });
        Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.TotalCount);
        var httpProduct = await _http.GetFromJsonAsync<JsonElement>($"/api/products/{created.Id}");
        Assert.Equal("Молоко", httpProduct.GetProperty("name").GetString());

        using var httpCreated = await _http.PostAsJsonAsync("/api/products", ValidHttpProduct());
        Assert.Equal(HttpStatusCode.Created, httpCreated.StatusCode);
        var all = await grpc.GetProductsAsync(new());
        Assert.Equal(2, all.TotalCount);
        Assert.Equal(1, all.PageNumber);
        Assert.Equal(10, all.PageSize);
    }

    [Fact]
    public async Task Grpc_OptionalZeroTypeIsAFilterButExplicitZeroPageIsInvalid()
    {
        using var channel = CreateChannel(_factory);
        var grpc = new GrpcContracts.ProductsService.ProductsServiceClient(channel);
        await grpc.CreateProductAsync(ValidGrpcProduct());
        var general = ValidGrpcProduct();
        general.Type = GrpcContracts.ProductType.General;
        var created = await grpc.CreateProductAsync(general);

        var page = await grpc.GetProductsAsync(new() { Type = GrpcContracts.ProductType.General });
        Assert.Equal(created.Id, Assert.Single(page.Items).Id);
        var error = await Assert.ThrowsAsync<RpcException>(async () =>
            await grpc.GetProductsAsync(new() { PageNumber = 0 }));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task Grpc_InvalidAndMissingIdsHaveDifferentStatuses()
    {
        using var channel = CreateChannel(_factory);
        var grpc = new GrpcContracts.ProductsService.ProductsServiceClient(channel);
        var invalid = await Assert.ThrowsAsync<RpcException>(async () => await grpc.GetProductAsync(new() { Id = "bad-id" }));
        var missing = await Assert.ThrowsAsync<RpcException>(async () => await grpc.GetProductAsync(new() { Id = Guid.NewGuid().ToString() }));
        Assert.Equal(StatusCode.InvalidArgument, invalid.StatusCode);
        Assert.Equal(StatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("1,000.00")]
    [InlineData("-1")]
    [InlineData("0")]
    public async Task Grpc_InvalidPriceReturnsInvalidArgument(string price)
    {
        using var channel = CreateChannel(_factory);
        var grpc = new GrpcContracts.ProductsService.ProductsServiceClient(channel);
        var request = ValidGrpcProduct();
        request.Price = price;
        var error = await Assert.ThrowsAsync<RpcException>(async () => await grpc.CreateProductAsync(request));
        Assert.Equal(StatusCode.InvalidArgument, error.StatusCode);
    }

    [Fact]
    public async Task Transcoding_CallsGrpcServiceWithJsonAndDateFilters()
    {
        using var created = await _http.PostAsJsonAsync("/v1/products", new
        {
            name = "Мыло",
            price = "123.45",
            weight = 0.1,
            type = "GENERAL",
            warehouseNumber = 3
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        var product = await _http.GetFromJsonAsync<JsonElement>($"/v1/products/{id}");
        Assert.Equal("123.45", product.GetProperty("price").GetString());
        var date = Uri.EscapeDataString(product.GetProperty("createdAt").GetString()!);
        var page = await _http.GetFromJsonAsync<JsonElement>(
            $"/v1/products?type=GENERAL&warehouseNumber=3&createdFrom={date}&createdTo={date}&pageSize=1");
        Assert.Equal(id, Assert.Single(page.GetProperty("items").EnumerateArray()).GetProperty("id").GetString());
        Assert.Equal(1, page.GetProperty("totalCount").GetInt32());
    }

    [Theory]
    [InlineData("/v1/products?pageNumber=0", HttpStatusCode.BadRequest)]
    [InlineData("/v1/products?pageSize=101", HttpStatusCode.BadRequest)]
    [InlineData("/v1/products/bad-id", HttpStatusCode.BadRequest)]
    [InlineData("/v1/products/00000000-0000-0000-0000-000000000001", HttpStatusCode.NotFound)]
    public async Task Transcoding_MapsGrpcErrorsToHttp(string path, HttpStatusCode expected)
    {
        using var response = await _http.GetAsync(path);
        Assert.Equal(expected, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(error.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Swagger_IncludesAllHttpAndTranscodedGrpcOperations()
    {
        var document = await _http.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = document.GetProperty("paths");
        foreach (var path in new[] { "/api/products", "/v1/products" })
        {
            Assert.True(paths.GetProperty(path).TryGetProperty("post", out _));
            Assert.True(paths.GetProperty(path).TryGetProperty("get", out _));
            Assert.True(paths.GetProperty(path + "/{id}").TryGetProperty("get", out _));
        }

        var parameters = paths.GetProperty("/v1/products").GetProperty("get").GetProperty("parameters")
            .EnumerateArray().ToDictionary(parameter => parameter.GetProperty("name").GetString()!);
        Assert.Equal(1, parameters["pageNumber"].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.Equal(10, parameters["pageSize"].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.Equal(100, parameters["pageSize"].GetProperty("schema").GetProperty("maximum").GetInt32());

        var productType = document.GetProperty("components").GetProperty("schemas")
            .GetProperty("GrpcCreateProduct").GetProperty("properties").GetProperty("type");
        var protoType = GrpcContracts.Product.Descriptor.FindFieldByNumber(GrpcContracts.Product.TypeFieldNumber).EnumType;
        Assert.Equal(protoType.Values.Select(value => value.Name),
            productType.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));

        using var ui = await _http.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
    }

    [Fact]
    public async Task UnhandledErrors_Return500AndInternalWithoutLeakingDetails()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IProductRepository>();
            services.AddSingleton<IProductRepository, FailingRepository>();
        }));
        using var http = factory.CreateClient();
        using var response = await http.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("secret-internal", await response.Content.ReadAsStringAsync());

        using var channel = CreateChannel(factory);
        var grpc = new GrpcContracts.ProductsService.ProductsServiceClient(channel);
        var error = await Assert.ThrowsAsync<RpcException>(async () => await grpc.GetProductsAsync(new()));
        Assert.Equal(StatusCode.Internal, error.StatusCode);
        Assert.DoesNotContain("secret-internal", error.Status.Detail);
    }

    public void Dispose()
    {
        _http.Dispose();
        _factory.Dispose();
    }

    private static object ValidHttpProduct() => new { name = "Молоко", price = 99.90m, weight = 1, type = "Food", warehouseNumber = 2 };
    private static GrpcContracts.CreateProductRequest ValidGrpcProduct() => new()
    {
        Name = "Молоко",
        Price = "99.90",
        Weight = 1,
        Type = GrpcContracts.ProductType.Food,
        WarehouseNumber = 2
    };

    private static GrpcChannel CreateChannel(WebApplicationFactory<Program> factory) =>
        GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions { HttpHandler = factory.Server.CreateHandler() });

    private sealed class FailingRepository : IProductRepository
    {
        public void Add(Product product) => throw new InvalidOperationException("secret-internal");
        public Product? GetById(Guid id) => throw new InvalidOperationException("secret-internal");
        public IEnumerable<Product> GetAll() => throw new InvalidOperationException("secret-internal");
        public IReadOnlyList<Product> Find(ProductQuery query) => throw new InvalidOperationException("secret-internal");
    }
}
