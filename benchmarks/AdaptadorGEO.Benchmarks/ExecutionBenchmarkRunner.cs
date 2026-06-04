using System.Data.Common;
using System.Diagnostics;
using AdaptadorGEO;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace AdaptadorGEO.Benchmarks;

internal static class ExecutionBenchmarkRunner
{
    public static async Task RunAsync(BenchmarkSettings settings)
    {
        var appSettings = BenchmarkAppSettings.Load(settings.ConfigPath);
        var providers = ExecutionProviderCatalog.Create(appSettings);

        Console.WriteLine("AdaptadorGEO execution benchmark");
        Console.WriteLine("Scope: live database execution through the translated SqlFragment");
        Console.WriteLine($"Config: {settings.ConfigPath}");
        Console.WriteLine($"Warmup: {settings.WarmupIterations}");
        Console.WriteLine($"Iterations: {settings.Iterations}");
        Console.WriteLine();

        if (providers.Count == 0)
        {
            Console.WriteLine("No provider connection strings were configured.");
            Console.WriteLine("Fill appsettings.json or set ADAPTORGEO_*_CONNECTION environment variables, then run again.");
            return;
        }

        foreach (var provider in providers)
        {
            try
            {
                if (provider.Kind == ExecutionProviderKind.SqlServer)
                {
                    await SqlServerDatabaseBootstrapper.EnsureDatabaseAsync(provider.ConnectionString);
                }

                await using var connection = provider.CreateConnection();
                await connection.OpenAsync();

                await ExecutionDatabaseBootstrapper.ResetAndSeedAsync(connection, provider, appSettings.Execution);

                Console.WriteLine(provider.Name);
                PrintHeader();

                var facade = GeoDatabase.For(connection);
                foreach (var scenario in ExecutionScenarioCatalog.Create())
                {
                    try
                    {
                        var expression = scenario.CreateExpression();
                        var fragment = facade.Translate(expression);
                        var predicateSql = scenario.GetPredicateSql(provider.Kind, fragment);

                        if (predicateSql is null)
                        {
                            PrintSkipped(provider.Name, scenario.Name, "not supported in live execution");
                            continue;
                        }

                        var result = await MeasureAsync(
                            connection,
                            provider,
                            fragment,
                            predicateSql,
                            scenario.Name,
                            settings,
                            appSettings.Execution.CommandTimeoutSeconds);
                        PrintResult(result);
                    }
                    catch (Exception ex)
                    {
                        PrintSkipped(provider.Name, scenario.Name, ex.Message);
                    }
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(provider.Name);
                Console.WriteLine($"Skipped: {ex.Message}");
                Console.WriteLine();
            }
        }
    }

    private static async Task<ExecutionBenchmarkResult> MeasureAsync(
        DbConnection connection,
        ExecutionProviderSpec provider,
        AdaptadorGEO.Sql.SqlFragment fragment,
        string predicateSql,
        string expressionName,
        BenchmarkSettings settings,
        int commandTimeoutSeconds)
    {
        for (var i = 0; i < settings.WarmupIterations; i++)
        {
            _ = await ExecuteCountAsync(connection, provider, fragment, predicateSql, commandTimeoutSeconds);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeAlloc = GC.GetTotalAllocatedBytes(precise: true);
        var start = Stopwatch.GetTimestamp();
        long resultSink = 0;

        for (var i = 0; i < settings.Iterations; i++)
        {
            resultSink ^= await ExecuteCountAsync(connection, provider, fragment, predicateSql, commandTimeoutSeconds);
        }

        var elapsed = Stopwatch.GetTimestamp() - start;
        var afterAlloc = GC.GetTotalAllocatedBytes(precise: true);

        GC.KeepAlive(resultSink);

        var seconds = (double)elapsed / Stopwatch.Frequency;
        var meanMilliseconds = seconds / settings.Iterations * 1000d;
        var allocBytes = (afterAlloc - beforeAlloc) / (double)settings.Iterations;

        return new ExecutionBenchmarkResult(
            provider.Name,
            expressionName,
            meanMilliseconds,
            allocBytes);
    }

    private static async Task<long> ExecuteCountAsync(
        DbConnection connection,
        ExecutionProviderSpec provider,
        AdaptadorGEO.Sql.SqlFragment fragment,
        string predicateSql,
        int timeoutSeconds)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = provider.BuildCountQuery(predicateSql);
        command.CommandTimeout = timeoutSeconds;

        foreach (var parameter in fragment.Parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = parameter.Name;
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }

        var scalar = await command.ExecuteScalarAsync();
        return scalar switch
        {
            null => 0,
            long longValue => longValue,
            int intValue => intValue,
            decimal decimalValue => (long)decimalValue,
            _ => Convert.ToInt64(scalar)
        };
    }

    private static void PrintHeader()
    {
        Console.WriteLine(
            $"{Pad("Expression", 24)} {Pad("Mean (ms/op)", 14)} {Pad("Alloc (B/op)", 14)}");
        Console.WriteLine(new string('-', 56));
    }

    private static void PrintResult(ExecutionBenchmarkResult result)
    {
        Console.WriteLine(
            $"{Pad(result.Expression, 24)} {Pad(result.MeanMilliseconds.ToString("0.00"), 14)} {Pad(result.AllocatedBytesPerOp.ToString("0.0"), 14)}");
    }

    private static void PrintSkipped(string providerName, string expressionName, string reason)
    {
        Console.WriteLine(
            $"{Pad(expressionName, 24)} {Pad("Skipped", 14)} {providerName}: {reason}");
    }

    private static string Pad(string value, int width) => value.Length >= width ? value[..width] : value.PadRight(width);
}

internal readonly record struct ExecutionBenchmarkResult(
    string Provider,
    string Expression,
    double MeanMilliseconds,
    double AllocatedBytesPerOp);

internal sealed class ExecutionProviderSpec
{
    public ExecutionProviderSpec(string name, ExecutionProviderKind kind, string connectionString)
    {
        Name = name;
        Kind = kind;
        ConnectionString = connectionString;
    }

    public string Name { get; }

    public ExecutionProviderKind Kind { get; }

    public string ConnectionString { get; }

    public DbConnection CreateConnection() =>
        Kind switch
        {
            ExecutionProviderKind.MySql => new MySqlConnection(ConnectionString),
            ExecutionProviderKind.SqlServer => new SqlConnection(ConnectionString),
            ExecutionProviderKind.PostgreSql => new NpgsqlConnection(ConnectionString),
            _ => throw new NotSupportedException($"Unsupported provider kind: {Kind}")
        };

    public string BuildCountQuery(string predicateSql) =>
        Kind switch
        {
            ExecutionProviderKind.MySql => $"SELECT COUNT(*) FROM `{TableName}` WHERE {predicateSql}",
            ExecutionProviderKind.SqlServer => $"SELECT COUNT_BIG(*) FROM [{TableName}] WHERE {predicateSql}",
            ExecutionProviderKind.PostgreSql => $"SELECT COUNT(*) FROM \"{TableName}\" WHERE {predicateSql}",
            _ => throw new NotSupportedException($"Unsupported provider kind: {Kind}")
        };

    public string TableName { get; init; } = "geo_features";
}

internal enum ExecutionProviderKind
{
    MySql,
    SqlServer,
    PostgreSql
}

internal static class ExecutionProviderCatalog
{
    public static IReadOnlyList<ExecutionProviderSpec> Create(BenchmarkAppSettings settings)
    {
        var providers = new List<ExecutionProviderSpec>();

        if (!string.IsNullOrWhiteSpace(settings.Connections.MySql))
        {
            providers.Add(new ExecutionProviderSpec("MySQL", ExecutionProviderKind.MySql, settings.Connections.MySql)
            {
                TableName = settings.Execution.TableName
            });
        }

        if (!string.IsNullOrWhiteSpace(settings.Connections.SqlServer))
        {
            providers.Add(new ExecutionProviderSpec("SQL Server", ExecutionProviderKind.SqlServer, settings.Connections.SqlServer)
            {
                TableName = settings.Execution.TableName
            });
        }

        if (!string.IsNullOrWhiteSpace(settings.Connections.PostgreSql))
        {
            providers.Add(new ExecutionProviderSpec("PostgreSQL", ExecutionProviderKind.PostgreSql, settings.Connections.PostgreSql)
            {
                TableName = settings.Execution.TableName
            });
        }

        return providers;
    }
}

internal static class ExecutionScenarioCatalog
{
    public static IReadOnlyList<ExecutionScenario> Create()
    {
        return
        [
            new ExecutionScenario(
                "Intersects(Point)",
                () => Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)),
                (providerKind, fragment) => providerKind == ExecutionProviderKind.SqlServer
                    ? $"({fragment.CommandText}) = 1"
                    : fragment.CommandText),
            new ExecutionScenario(
                "Contains(Polygon)",
                () => Geo.Column("area").Contains(
                Geo.Polygon(
                    Geo.Point(-23.55, -46.63),
                    Geo.Point(-23.56, -46.64),
                    Geo.Point(-23.57, -46.65),
                    Geo.Point(-23.55, -46.63))),
                (providerKind, fragment) => providerKind == ExecutionProviderKind.SqlServer
                    ? $"({fragment.CommandText}) = 1"
                    : fragment.CommandText),
            new ExecutionScenario(
                "Within(MultiPolygon)",
                () => Geo.Column("area").Within(
                Geo.MultiPolygon(
                    Geo.Polygon(
                        Geo.Point(-23.55, -46.63),
                        Geo.Point(-23.56, -46.64),
                        Geo.Point(-23.57, -46.65),
                        Geo.Point(-23.55, -46.63)))),
                (providerKind, fragment) => providerKind == ExecutionProviderKind.SqlServer
                    ? $"({fragment.CommandText}) = 1"
                    : fragment.CommandText),
            new ExecutionScenario(
                "Distance(Point)",
                () => Geo.Column("area").Distance(Geo.Point(-23.55052, -46.63331)),
                (providerKind, fragment) => providerKind == ExecutionProviderKind.SqlServer
                    ? $"({fragment.CommandText}) >= 0"
                    : $"({fragment.CommandText}) >= 0"),
            new ExecutionScenario("Buffer(250)", () => Geo.Column("area").Buffer(250), (_, _) => null)
        ];
    }
}

internal sealed record ExecutionScenario(
    string Name,
    Func<AdaptadorGEO.Spatial.GeoExpression> CreateExpression,
    Func<ExecutionProviderKind, AdaptadorGEO.Sql.SqlFragment, string?> PredicateSqlFactory = null!)
{
    public string? GetPredicateSql(ExecutionProviderKind providerKind, AdaptadorGEO.Sql.SqlFragment fragment) =>
        PredicateSqlFactory is null ? fragment.CommandText : PredicateSqlFactory(providerKind, fragment);
}
