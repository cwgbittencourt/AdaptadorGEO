using System.Data.Common;
namespace AdaptadorGEO.Benchmarks;

internal static class ExecutionDatabaseBootstrapper
{
    public static async Task ResetAndSeedAsync(
        DbConnection connection,
        ExecutionProviderSpec provider,
        ExecutionOptions options)
    {
        await DropTableAsync(connection, provider);
        if (provider.Kind == ExecutionProviderKind.PostgreSql)
        {
            await EnsurePostgisAsync(connection);
        }
        await CreateTableAsync(connection, provider, options.TableName);
        await SeedAsync(connection, provider, options);
    }

    private static async Task EnsurePostgisAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE EXTENSION IF NOT EXISTS postgis";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropTableAsync(DbConnection connection, ExecutionProviderSpec provider)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = provider.Kind switch
        {
            ExecutionProviderKind.MySql => $"DROP TABLE IF EXISTS `{provider.TableName}`",
            ExecutionProviderKind.SqlServer => $"DROP TABLE IF EXISTS [{provider.TableName}]",
            ExecutionProviderKind.PostgreSql => $"DROP TABLE IF EXISTS \"{provider.TableName}\"",
            _ => throw new NotSupportedException($"Unsupported provider kind: {provider.Kind}")
        };

        await command.ExecuteNonQueryAsync();
    }

    private static async Task CreateTableAsync(DbConnection connection, ExecutionProviderSpec provider, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = provider.Kind switch
        {
            ExecutionProviderKind.MySql => $@"
CREATE TABLE `{tableName}` (
    `id` BIGINT NOT NULL AUTO_INCREMENT,
    `name` VARCHAR(100) NOT NULL,
    `area` GEOMETRY NOT NULL,
    PRIMARY KEY (`id`)
)",
            ExecutionProviderKind.SqlServer => $@"
CREATE TABLE [{tableName}] (
    [id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [name] NVARCHAR(100) NOT NULL,
    [area] GEOGRAPHY NOT NULL
)",
            ExecutionProviderKind.PostgreSql => $@"
CREATE TABLE ""{tableName}"" (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    area geometry NOT NULL
)",
            _ => throw new NotSupportedException($"Unsupported provider kind: {provider.Kind}")
        };

        await command.ExecuteNonQueryAsync();
    }

    private static async Task SeedAsync(DbConnection connection, ExecutionProviderSpec provider, ExecutionOptions options)
    {
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            for (var i = 0; i < options.SeedRows; i++)
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = provider.Kind switch
                {
                    ExecutionProviderKind.MySql => $"INSERT INTO `{provider.TableName}` (`name`, `area`) VALUES (@name, ST_GeomFromText(@wkt, 4326))",
                    ExecutionProviderKind.SqlServer => $"INSERT INTO [{provider.TableName}] ([name], [area]) VALUES (@name, geography::STGeomFromText(@wkt, 4326))",
                    ExecutionProviderKind.PostgreSql => $"INSERT INTO \"{provider.TableName}\" (name, area) VALUES (@name, ST_GeomFromText(@wkt, 4326))",
                    _ => throw new NotSupportedException($"Unsupported provider kind: {provider.Kind}")
                };

                AddParameter(command, "@name", $"seed-{i}");
                AddParameter(command, "@wkt", CreateSeedWkt(i));

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void AddParameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string CreateSeedWkt(int index)
    {
        var offset = index * 0.00005d;
        var lon = -46.65d + offset;
        var lat = -23.60d + offset;

        return FormattableString.Invariant(
            $"POLYGON(({lon} {lat}, {lon + 0.001} {lat}, {lon + 0.001} {lat + 0.001}, {lon} {lat + 0.001}, {lon} {lat}))");
    }
}
