using System.Data;
using Microsoft.Extensions.Configuration;
using MonoSlice.Shared.Abstractions.Persistence;
using Npgsql;

namespace MonoSlice.Shared.Infrastructure.Persistence;

/// <summary>
/// Npgsql implementation of ISqlConnectionFactory for PostgreSQL.
/// </summary>
public sealed class NpgsqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                            configuration.GetConnectionString("OrdersDb") ??
                            "Host=localhost;Database=monoslice_db;Username=postgres;Password=postgres";
    }

    public NpgsqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
