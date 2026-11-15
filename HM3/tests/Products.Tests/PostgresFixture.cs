using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Products.Tests;

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly string _databaseName = "products_test_" + Guid.NewGuid().ToString("N");
    private readonly string _serverConnection = Environment.GetEnvironmentVariable("PRODUCTS_TEST_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=postgres;Username=products;Password=products;Timeout=5";
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
            Pooling = false
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
        await using var connection = new NpgsqlConnection(ConnectionString);
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
