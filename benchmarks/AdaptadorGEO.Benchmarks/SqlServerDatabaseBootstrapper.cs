using Microsoft.Data.SqlClient;

namespace AdaptadorGEO.Benchmarks;

internal static class SqlServerDatabaseBootstrapper
{
    public static async Task EnsureDatabaseAsync(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return;
        }

        builder.InitialCatalog = "master";
        builder["Database"] = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        var escapedDatabaseName = databaseName.Replace("'", "''");
        var escapedDatabaseIdentifier = databaseName.Replace("]", "]]");
        command.CommandText = $@"
IF DB_ID(N'{escapedDatabaseName}') IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE DATABASE [{escapedDatabaseIdentifier}]';
    EXEC(@sql);
END";

        await command.ExecuteNonQueryAsync();
    }
}
