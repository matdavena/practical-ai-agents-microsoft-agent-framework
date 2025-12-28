// ============================================================================
// SqliteConnectionFactory
// ============================================================================
// Creates SQLite database connections using Microsoft.Data.Sqlite.
// Handles connection string configuration and file path management.
// ============================================================================

using System.Data;
using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Infrastructure.Data;

/// <summary>
/// Factory for creating SQLite database connections.
/// </summary>
public class SqliteConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Creates a new SQLite connection factory with the specified connection string.
    /// </summary>
    /// <param name="connectionString">SQLite connection string.</param>
    public SqliteConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Creates a new SQLite connection factory for a database file.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>A new SqliteConnectionFactory instance.</returns>
    public static SqliteConnectionFactory CreateForFile(string databasePath)
    {
        // Ensure the directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        return new SqliteConnectionFactory(connectionString);
    }

    /// <summary>
    /// Creates a factory for an in-memory database (useful for testing).
    /// </summary>
    /// <returns>A new SqliteConnectionFactory for in-memory database.</returns>
    public static SqliteConnectionFactory CreateInMemory()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = ":memory:",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        return new SqliteConnectionFactory(connectionString);
    }

    /// <inheritdoc />
    public string ConnectionString => _connectionString;

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
