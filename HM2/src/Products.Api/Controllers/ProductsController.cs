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
    public ActionResult<CreateProductResponse> Create(CreateProductRequest request)
    {
        var id = service.Create(request);
        return CreatedAtAction(nameof(GetById), new { id }, new CreateProductResponse(id));
    }

    [HttpGet]
    [ProducesResponseType<PagedProducts>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<PagedProducts> GetProducts([FromQuery] ProductQuery query) => service.GetProducts(query);

    [HttpGet("{id}")]
    [ProducesResponseType<Product>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public ActionResult<Product> GetById(Guid id) => service.GetById(id);
}
