using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Products.Tests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _databaseName = "products_test_" + Guid.NewGuid().ToString("N");
    private readonly string _serverConnection = new NpgsqlConnectionStringBuilder(
        Environment.GetEnvironmentVariable("PRODUCTS_TEST_CONNECTION")
        ?? "Host=127.0.0.1;Port=5433;Database=postgres;Username=products;Password=products;Timeout=5")
    {
        Pooling = false
    }.ConnectionString;
    private readonly WebApplicationFactory<Program> _factory = new();
    private bool _databaseCreated;

    public WebApplicationFactory<Program> Api { get; private set; } = null!;
    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(_serverConnection);
        await connection.ExecuteAsync($"CREATE DATABASE {_databaseName}");
        _databaseCreated = true;

        ConnectionString = new NpgsqlConnectionStringBuilder(_serverConnection)
        {
            Database = _databaseName,
            Pooling = true
        }.ConnectionString;
        Api = _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Products"] = ConnectionString
            })));
        _ = Api.Server;
    }

    public async Task ResetAsync()
    {
        var dataSource = Api.Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("TRUNCATE products");
    }

    public async Task DisposeAsync()
    {
        try
        {
            await _factory.DisposeAsync();
        }
        finally
        {
            if (_databaseCreated)
            {
                await using var connection = new NpgsqlConnection(_serverConnection);
                await connection.ExecuteAsync($"DROP DATABASE {_databaseName} WITH (FORCE)");
            }
        }
    }
}
