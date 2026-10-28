using Products.Api.Configuration;
using Products.Api.Grpc;
using Products.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => options.TimestampFormat = "HH:mm:ss ");

builder.Services
    .AddProductServices()
    .AddProductTransports()
    .AddProductSwagger();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGrpcService<ProductsGrpcService>();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Entry point for integration tests using WebApplicationFactory.
public partial class Program;
