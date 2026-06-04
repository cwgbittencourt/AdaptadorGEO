using System.Data.Common;
using System.Diagnostics;
using System.Linq.Expressions;
using AdaptadorGEO;
using AdaptadorGEO.Integration.Dapper;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AdaptadorGEO.Benchmarks;

internal static class FrameworkComparisonBenchmarkRunner
{
    public static async Task RunAsync(BenchmarkSettings settings)
    {
        var appSettings = BenchmarkAppSettings.Load(settings.ConfigPath);
        var sqlServerConnectionString = appSettings.Connections.SqlServer;

        if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
        {
            Console.WriteLine("No SQL Server connection string was configured.");
            Console.WriteLine("Set appsettings.json or ADAPTORGEO_SQLSERVER_CONNECTION, then run again.");
            return;
        }

        var provider = new ExecutionProviderSpec(
            "SQL Server",
            ExecutionProviderKind.SqlServer,
            sqlServerConnectionString)
        {
            TableName = appSettings.Execution.TableName
        };

        await SqlServerDatabaseBootstrapper.EnsureDatabaseAsync(provider.ConnectionString);

        await using var connection = provider.CreateConnection();
        await connection.OpenAsync();

        await ExecutionDatabaseBootstrapper.ResetAndSeedAsync(connection, provider, appSettings.Execution);

        var facade = GeoDatabase.For(connection);
        await using var efContext = CreateEfCoreContext(connection, provider.TableName, appSettings.Execution.CommandTimeoutSeconds);

        Console.WriteLine("AdaptadorGEO framework comparison benchmark");
        Console.WriteLine("Scope: SQL Server execution baseline across AdaptadorGEO, Dapper, and EF Core + NetTopologySuite");
        Console.WriteLine($"Config: {settings.ConfigPath}");
        Console.WriteLine($"Warmup: {settings.WarmupIterations}");
        Console.WriteLine($"Iterations: {settings.Iterations}");
        Console.WriteLine();

        PrintHeader();

        foreach (var scenario in ExecutionScenarioCatalog.Create())
        {
            var expression = scenario.CreateExpression();
            var fragment = facade.Translate(expression);
            var predicateSql = scenario.GetPredicateSql(ExecutionProviderKind.SqlServer, fragment);

            if (predicateSql is null)
            {
                PrintSkipped("AdaptadorGEO", scenario.Name, "not supported in execution baseline");
                PrintSkipped("Dapper", scenario.Name, "not supported in execution baseline");
                PrintSkipped("EF Core", scenario.Name, "not supported in execution baseline");
                continue;
            }

            var efPredicate = BuildEfCorePredicate(scenario.Name);

            if (settings.DumpSql)
            {
                PrintSqlComparison(
                    scenario.Name,
                    provider.BuildCountQuery(predicateSql),
                    fragment.Parameters,
                    BuildEfCoreSql(efContext, efPredicate));
            }

            var adaptadorResult = await MeasureAsync(
                async () => await ExecuteAdaptadorAsync(connection, provider, fragment, predicateSql, appSettings.Execution.CommandTimeoutSeconds),
                "AdaptadorGEO",
                scenario.Name,
                settings);
            PrintResult(adaptadorResult);

            var dapperResult = await MeasureAsync(
                async () => await ExecuteDapperAsync(connection, provider, fragment, predicateSql, appSettings.Execution.CommandTimeoutSeconds),
                "Dapper",
                scenario.Name,
                settings);
            PrintResult(dapperResult);

            var efCoreResult = await MeasureAsync(
                async () => await ExecuteEfCoreAsync(efContext, efPredicate, appSettings.Execution.CommandTimeoutSeconds),
                "EF Core",
                scenario.Name,
                settings);
            PrintResult(efCoreResult);
        }

    }

    private static async Task<ComparisonBenchmarkResult> MeasureAsync(
        Func<Task<long>> action,
        string framework,
        string expression,
        BenchmarkSettings settings)
    {
        for (var i = 0; i < settings.WarmupIterations; i++)
        {
            _ = await action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeAlloc = GC.GetTotalAllocatedBytes(precise: true);
        var start = Stopwatch.GetTimestamp();
        long sink = 0;

        for (var i = 0; i < settings.Iterations; i++)
        {
            sink ^= await action();
        }

        var elapsed = Stopwatch.GetTimestamp() - start;
        var afterAlloc = GC.GetTotalAllocatedBytes(precise: true);

        GC.KeepAlive(sink);

        var seconds = (double)elapsed / Stopwatch.Frequency;
        var meanMilliseconds = seconds / settings.Iterations * 1000d;
        var allocBytes = (afterAlloc - beforeAlloc) / (double)settings.Iterations;

        return new ComparisonBenchmarkResult(framework, expression, meanMilliseconds, allocBytes);
    }

    private static async Task<long> ExecuteAdaptadorAsync(
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
        return Convert.ToInt64(scalar ?? 0L);
    }

    private static async Task<long> ExecuteDapperAsync(
        DbConnection connection,
        ExecutionProviderSpec provider,
        AdaptadorGEO.Sql.SqlFragment fragment,
        string predicateSql,
        int timeoutSeconds)
    {
        var sql = provider.BuildCountQuery(predicateSql);
        return await connection.QuerySingleAsync<long>(
            sql,
            fragment.ToDynamicParameters(),
            commandTimeout: timeoutSeconds);
    }

    private static async Task<long> ExecuteEfCoreAsync(
        SqlServerSpatialBenchmarkContext context,
        Expression<Func<GeoFeature, bool>> predicate,
        int timeoutSeconds)
    {
        context.Database.SetCommandTimeout(timeoutSeconds);
        return await context.Features.CountAsync(predicate);
    }

    private static SqlServerSpatialBenchmarkContext CreateEfCoreContext(
        DbConnection connection,
        string tableName,
        int timeoutSeconds)
    {
        var options = new DbContextOptionsBuilder<SqlServerSpatialBenchmarkContext>()
            .UseSqlServer(connection, sql => sql.UseNetTopologySuite())
            .Options;

        var context = new SqlServerSpatialBenchmarkContext(options, tableName);
        context.Database.SetCommandTimeout(timeoutSeconds);
        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        return context;
    }

    private static Expression<Func<GeoFeature, bool>> BuildEfCorePredicate(string scenarioName)
    {
        var point = ComparisonGeometries.Point;
        var polygon = ComparisonGeometries.Polygon;
        var multiPolygon = ComparisonGeometries.MultiPolygon;

        return scenarioName switch
        {
            "Intersects(Point)" => feature => feature.Area.Intersects(point),
            "Contains(Polygon)" => feature => feature.Area.Contains(polygon),
            "Within(MultiPolygon)" => feature => feature.Area.Within(multiPolygon),
            "Distance(Point)" => feature => feature.Area.Distance(point) >= 0,
            _ => throw new NotSupportedException($"Unsupported scenario: {scenarioName}")
        };
    }

    private static void PrintHeader()
    {
        Console.WriteLine(
            $"{Pad("Framework", 14)} {Pad("Expression", 24)} {Pad("Mean (ms/op)", 14)} {Pad("Alloc (B/op)", 14)}");
        Console.WriteLine(new string('-', 80));
    }

    private static void PrintResult(ComparisonBenchmarkResult result)
    {
        Console.WriteLine(
            $"{Pad(result.Framework, 14)} {Pad(result.Expression, 24)} {Pad(result.MeanMilliseconds.ToString("0.00"), 14)} {Pad(result.AllocatedBytesPerOp.ToString("0.0"), 14)}");
    }

    private static void PrintSkipped(string framework, string expression, string reason)
    {
        Console.WriteLine(
            $"{Pad(framework, 14)} {Pad(expression, 24)} {Pad("Skipped", 14)} {reason}");
    }

    private static void PrintSqlComparison(
        string expressionName,
        string adaptadorSql,
        IReadOnlyList<AdaptadorGEO.Sql.SqlParameter> parameters,
        string efCoreSql)
    {
        Console.WriteLine();
        Console.WriteLine($"SQL for {expressionName}");
        Console.WriteLine("- AdaptadorGEO / Dapper");
        Console.WriteLine(adaptadorSql);

        if (parameters.Count > 0)
        {
            Console.WriteLine("Parameters:");
            foreach (var parameter in parameters)
            {
                Console.WriteLine($"  {parameter.Name} = {FormatParameterValue(parameter.Value)}");
            }
        }

        Console.WriteLine("- EF Core");
        Console.WriteLine(efCoreSql);
        Console.WriteLine();
    }

    private static string BuildEfCoreSql(SqlServerSpatialBenchmarkContext context, Expression<Func<GeoFeature, bool>> predicate)
    {
        return context.Features.Where(predicate).ToQueryString();
    }

    private static string FormatParameterValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string text => text,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Pad(string value, int width) => value.Length >= width ? value[..width] : value.PadRight(width);
}

internal readonly record struct ComparisonBenchmarkResult(
    string Framework,
    string Expression,
    double MeanMilliseconds,
    double AllocatedBytesPerOp);

internal sealed class SqlServerSpatialBenchmarkContext : DbContext
{
    private readonly string _tableName;

    public SqlServerSpatialBenchmarkContext(DbContextOptions<SqlServerSpatialBenchmarkContext> options, string tableName)
        : base(options)
    {
        _tableName = tableName;
    }

    public DbSet<GeoFeature> Features => Set<GeoFeature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GeoFeature>();
        entity.ToTable(_tableName);
        entity.HasKey(feature => feature.Id);
        entity.Property(feature => feature.Name).HasColumnName("name");
        entity.Property(feature => feature.Area).HasColumnName("area").HasColumnType("geography");
    }
}

internal sealed class GeoFeature
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public NetTopologySuite.Geometries.Geometry Area { get; set; } = null!;
}

internal static class ComparisonGeometries
{
    private static readonly GeometryFactory Factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static Point Point { get; } = Factory.CreatePoint(new Coordinate(-46.63331, -23.55052));

    public static Polygon Polygon { get; } = Factory.CreatePolygon(new[]
    {
        new Coordinate(-46.63, -23.55),
        new Coordinate(-46.64, -23.56),
        new Coordinate(-46.65, -23.57),
        new Coordinate(-46.63, -23.55)
    });

    public static MultiPolygon MultiPolygon { get; } = Factory.CreateMultiPolygon(new[] { Polygon });
}
