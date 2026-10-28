using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.OpenApi.Models;
using Products.Api.Application;
using Products.Api.Application.Validation;
using Products.Api.Grpc;
using Products.Api.Infrastructure;
using Products.Api.OpenApi;

namespace Products.Api.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductServices(this IServiceCollection services)
    {
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<ProductService>();
        services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

        return services;
    }

    public static IServiceCollection AddProductTransports(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddGrpc(options =>
        {
            options.Interceptors.Add<LoggingInterceptor>();
            options.Interceptors.Add<ExceptionInterceptor>();
        }).AddJsonTranscoding();

        return services;
    }

    public static IServiceCollection AddProductSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Хранение информации о товарах",
                Version = "v1",
                Description = "HTTP API и JSON-представление gRPC. Данные хранятся в памяти процесса."
            });
            options.DocumentFilter<GrpcTranscodingDocumentFilter>();
        });

        return services;
    }
}
