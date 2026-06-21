using DrawTogether.Server.Configuration;
using MySqlConnector;

namespace DrawTogether.Server.Data;

public sealed class MySqlDatabase
{
    private readonly string _connectionString;

    public MySqlDatabase(DatabaseOptions options)
    {
        _connectionString = options.ConnectionString;

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new ArgumentException("Database connection string is required.");
        }
    }

    public async Task<MySqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
