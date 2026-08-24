using System.Data;

namespace MonoSlice.Shared.Abstractions.Persistence;

/// <summary>
/// Factory for creating and opening database connections for high-performance Dapper read queries.
/// </summary>
public interface ISqlConnectionFactory
{
    /// <summary>
    /// Creates a new closed database connection.
    /// </summary>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates and asynchronously opens a new database connection.
    /// </summary>
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
