using Microsoft.AspNetCore.Mvc;
using Products.Api.Application;
using Products.Api.Domain;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreateProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateProductResponse>> Create(
        ProductDetails request, CancellationToken cancellationToken)
    {
        var id = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new CreateProductResponse(id));
    }

    [HttpGet]
    [ProducesResponseType<PagedProducts>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedProducts>> GetProducts(
        [FromQuery] ProductQuery query, CancellationToken cancellationToken) =>
        await service.GetProductsAsync(query, cancellationToken);

    [HttpGet("{id}")]
    [ProducesResponseType<Product>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> GetById(Guid id, CancellationToken cancellationToken) =>
        await service.GetByIdAsync(id, cancellationToken);

    [HttpPut("{id}")]
    [ProducesResponseType<Product>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Product>> Update(
        Guid id, ProductDetails request, CancellationToken cancellationToken) =>
        await service.UpdateAsync(id, request, cancellationToken);
}
