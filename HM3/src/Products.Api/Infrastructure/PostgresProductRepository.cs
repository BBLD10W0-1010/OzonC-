using System.Data;
using Dapper;
using Npgsql;
using Products.Api.Application;
using Products.Api.Domain;

namespace Products.Api.Infrastructure;

public sealed class PostgresProductRepository(NpgsqlDataSource dataSource) : IProductRepository
{
    private const string Columns = """
        id, name, price, weight, type, created_at AS CreatedAt, warehouse_number AS WarehouseNumber
        """;

    public async Task<Guid> AddAsync(Product product, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO products (id, name, price, weight, type, created_at, warehouse_number)
            VALUES (@Id, @Name, @Price, @Weight, @Type, @CreatedAt, @WarehouseNumber)
            RETURNING id
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleAsync<Guid>(new CommandDefinition(sql, new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Weight,
            Type = (int)product.Type,
            CreatedAt = product.CreatedAt.ToUniversalTime(),
            product.WarehouseNumber
        }, cancellationToken: cancellationToken));
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = $"SELECT {Columns} FROM products WHERE id = @Id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        return row?.ToProduct();
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> FindAsync(
        ProductQuery query, CancellationToken cancellationToken)
    {
        var (where, parameters) = BuildFilter(query);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            IsolationLevel.RepeatableRead, cancellationToken);

        var totalCount = await connection.QuerySingleAsync<int>(new CommandDefinition(
            $"SELECT COUNT(*) FROM products {where}", parameters, transaction, cancellationToken: cancellationToken));
        if (query.Offset >= totalCount)
        {
            await transaction.CommitAsync(cancellationToken);
            return ([], totalCount);
        }

        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", query.Offset);
        var sql = $"""
            SELECT {Columns} FROM products {where}
            ORDER BY created_at, id
            LIMIT @PageSize OFFSET @Offset
            """;
        var rows = await connection.QueryAsync<ProductRow>(
            new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return (rows.Select(row => row.ToProduct()).ToArray(), totalCount);
    }

    private static (string Where, DynamicParameters Parameters) BuildFilter(ProductQuery query)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (query.Type is { } type)
        {
            conditions.Add("type = @Type");
            parameters.Add("Type", (int)type);
        }

        if (query.WarehouseNumber is { } warehouse)
        {
            conditions.Add("warehouse_number = @Warehouse");
            parameters.Add("Warehouse", warehouse);
        }

        if (query.CreatedFrom is { } from)
        {
            conditions.Add("created_at >= @CreatedFrom");
            parameters.Add("CreatedFrom", from.ToUniversalTime());
        }

        if (query.CreatedTo is { } to)
        {
            conditions.Add("created_at <= @CreatedTo");
            parameters.Add("CreatedTo", to.ToUniversalTime());
        }

        var where = conditions.Count == 0 ? "" : "WHERE " + string.Join(" AND ", conditions);
        return (where, parameters);
    }
}
