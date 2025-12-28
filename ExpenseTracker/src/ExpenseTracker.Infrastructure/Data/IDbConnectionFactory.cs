// ============================================================================
// IDbConnectionFactory Interface
// ============================================================================
// Abstraction for creating database connections.
// Allows switching database providers without changing repository code.
// ============================================================================

using System.Data;

namespace ExpenseTracker.Infrastructure.Data;

/// <summary>
/// Factory interface for creating database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection.
    /// The caller is responsible for disposing the connection.
    /// </summary>
    IDbConnection CreateConnection();

    /// <summary>
    /// Gets the connection string used by this factory.
    /// </summary>
    string ConnectionString { get; }
}
