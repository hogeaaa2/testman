using Microsoft.Data.Sqlite;

namespace Testman.Core.Persistence;

public static class SqliteConnectionFactory
{
    public static SqliteConnection Open(string databasePath, bool pooling = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            ForeignKeys = true,
            Pooling = pooling,
        }.ToString());
        connection.Open();
        return connection;
    }
}
