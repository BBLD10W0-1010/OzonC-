using FluentMigrator.Runner;
using Npgsql;
using Products.Api.Application;
using Products.Api.Infrastructure;
using Products.Api.Infrastructure.Migrations;

namespace Products.Api.Configuration;

public static class ProductStorageExtensions
{
    public static IServiceCollection AddProductStorage(this IServiceCollection services)
    {
        services.AddSingleton(provider => NpgsqlDataSource.Create(GetConnectionString(provider)));

        services.AddSingleton<IProductRepository, PostgresProductRepository>();
        services.AddFluentMigratorCore().ConfigureRunner(runner => runner
            .AddPostgres()
            .WithGlobalConnectionString(GetConnectionString)
            .ScanIn(typeof(CreateProducts).Assembly).For.Migrations());

        return services;
    }

    private static string GetConnectionString(IServiceProvider provider) =>
        provider.GetRequiredService<IConfiguration>().GetConnectionString("Products")
        ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Products.");
}
