using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using AdaptadorGEO;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace AdaptadorGEO.Benchmarks;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var settings = BenchmarkSettings.Parse(args);

        if (settings.Mode == BenchmarkRunMode.Execution)
        {
            await ExecutionBenchmarkRunner.RunAsync(settings);
        }
        else if (settings.Mode == BenchmarkRunMode.Comparison)
        {
            await FrameworkComparisonBenchmarkRunner.RunAsync(settings);
        }
        else
        {
            var cases = BenchmarkCases.Create();

            Console.WriteLine("AdaptadorGEO translation benchmark");
            Console.WriteLine("Scope: translation only, no database execution");
            Console.WriteLine($"Warmup: {settings.WarmupIterations}");
            Console.WriteLine($"Iterations: {settings.Iterations}");
            Console.WriteLine();

            PrintHeader();

            foreach (var benchmarkCase in cases)
            {
                var result = Measure(benchmarkCase, settings);
                PrintResult(result);
            }
        }
    }

    private static BenchmarkResult Measure(BenchmarkCase benchmarkCase, BenchmarkSettings settings)
    {
        for (var i = 0; i < settings.WarmupIterations; i++)
        {
            GC.KeepAlive(benchmarkCase.Action());
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();

        var sink = 0;
        for (var i = 0; i < settings.Iterations; i++)
        {
            var fragment = benchmarkCase.Action();
            sink ^= fragment.CommandText.Length;
            sink ^= fragment.Parameters.Count;
        }

        var elapsed = Stopwatch.GetTimestamp() - start;
        var afterAlloc = GC.GetAllocatedBytesForCurrentThread();

        GC.KeepAlive(sink);

        var seconds = (double)elapsed / Stopwatch.Frequency;
        var meanMicroseconds = seconds / settings.Iterations * 1_000_000d;
        var allocBytes = (afterAlloc - beforeAlloc) / (double)settings.Iterations;

        return new BenchmarkResult(
            benchmarkCase.Provider,
            benchmarkCase.Expression,
            benchmarkCase.Scenario,
            meanMicroseconds,
            allocBytes);
    }

    private static void PrintHeader()
    {
        Console.WriteLine(
            $"{Pad("Provider", 12)} {Pad("Expression", 20)} {Pad("Scenario", 24)} {Pad("Mean (us/op)", 14)} {Pad("Alloc (B/op)", 14)}");
        Console.WriteLine(new string('-', 90));
    }

    private static void PrintResult(BenchmarkResult result)
    {
        Console.WriteLine(
            $"{Pad(result.Provider, 12)} {Pad(result.Expression, 20)} {Pad(result.Scenario, 24)} {Pad(result.MeanMicroseconds.ToString("0.00"), 14)} {Pad(result.AllocatedBytesPerOp.ToString("0.0"), 14)}");
    }

    private static string Pad(string value, int width) => value.Length >= width ? value[..width] : value.PadRight(width);
}

internal enum BenchmarkRunMode
{
    Translation,
    Execution,
    Comparison
}

internal readonly record struct BenchmarkSettings(
    BenchmarkRunMode Mode,
    int WarmupIterations,
    int Iterations,
    string ConfigPath,
    bool DumpSql)
{
    public static BenchmarkSettings Parse(string[] args)
    {
        var mode = BenchmarkRunMode.Translation;
        var warmup = 1000;
        var iterations = 10_000;
        var configPath = @"benchmarks/AdaptadorGEO.Benchmarks/appsettings.json";
        var dumpSql = false;

        foreach (var arg in args)
        {
            if (TryReadString(arg, "--mode=", out var parsedMode))
            {
                mode = parsedMode.Equals("execution", StringComparison.OrdinalIgnoreCase)
                    ? BenchmarkRunMode.Execution
                    : parsedMode.Equals("comparison", StringComparison.OrdinalIgnoreCase)
                        ? BenchmarkRunMode.Comparison
                    : BenchmarkRunMode.Translation;
                continue;
            }

            if (TryReadInt(arg, "--warmup=", out var parsedWarmup))
            {
                warmup = parsedWarmup;
                continue;
            }

            if (TryReadInt(arg, "--iterations=", out var parsedIterations))
            {
                iterations = parsedIterations;
                continue;
            }

            if (TryReadString(arg, "--config=", out var parsedConfigPath))
            {
                configPath = parsedConfigPath;
                continue;
            }

            if (arg.Equals("--dump-sql", StringComparison.OrdinalIgnoreCase))
            {
                dumpSql = true;
            }
        }

        return new BenchmarkSettings(mode, warmup, iterations, configPath, dumpSql);
    }

    private static bool TryReadInt(string arg, string prefix, out int value)
    {
        value = 0;
        if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(arg[prefix.Length..], out value);
    }

    private static bool TryReadString(string arg, string prefix, out string value)
    {
        value = string.Empty;
        if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = arg[prefix.Length..];
        return !string.IsNullOrWhiteSpace(value);
    }
}

internal readonly record struct BenchmarkCase(
    string Provider,
    string Expression,
    string Scenario,
    Func<AdaptadorGEO.Sql.SqlFragment> Action);

internal readonly record struct BenchmarkResult(
    string Provider,
    string Expression,
    string Scenario,
    double MeanMicroseconds,
    double AllocatedBytesPerOp);

internal static class BenchmarkCases
{
    public static IReadOnlyList<BenchmarkCase> Create()
    {
        var cases = new List<BenchmarkCase>();

        cases.AddRange(CreateForProvider(
            "MySQL",
            new MySqlConnectionStub(),
            new MySqlSpatialTranslator()));

        cases.AddRange(CreateForProvider(
            "SQL Server",
            new SqlServerConnectionStub(),
            new SqlServerSpatialTranslator()));

        cases.AddRange(CreateForProvider(
            "PostgreSQL",
            new NpgsqlConnectionStub(),
            new PostgreSqlSpatialTranslator()));

        return cases;
    }

    private static IEnumerable<BenchmarkCase> CreateForProvider(
        string providerName,
        DbConnection connection,
        object directTranslator)
    {
        var facade = GeoDatabase.For(connection);
        var point = Geo.Point(-23.55052, -46.63331);
        var polygon = Geo.Polygon(
            Geo.Point(-23.55, -46.63),
            Geo.Point(-23.56, -46.64),
            Geo.Point(-23.57, -46.65),
            Geo.Point(-23.55, -46.63));
        var multiPolygon = Geo.MultiPolygon(polygon);

        var expressions = new[]
        {
            ("Intersects(Point)", (Func<AdaptadorGEO.Sql.SqlFragment>)(() => facade.Translate(Geo.Column("area").Intersects(point)))),
            ("Contains(Polygon)", (Func<AdaptadorGEO.Sql.SqlFragment>)(() => facade.Translate(Geo.Column("area").Contains(polygon)))),
            ("Within(MultiPolygon)", (Func<AdaptadorGEO.Sql.SqlFragment>)(() => facade.Translate(Geo.Column("area").Within(multiPolygon)))),
            ("Distance(Point)", (Func<AdaptadorGEO.Sql.SqlFragment>)(() => facade.Translate(Geo.Column("area").Distance(point)))),
            ("Buffer(250)", (Func<AdaptadorGEO.Sql.SqlFragment>)(() => facade.Translate(Geo.Column("area").Buffer(250))))
        };

        foreach (var (expressionName, facadeAction) in expressions)
        {
            yield return new BenchmarkCase(providerName, expressionName, "Facade", facadeAction);

            yield return new BenchmarkCase(providerName, expressionName, "Direct", expressionName switch
            {
                "Intersects(Point)" => () => InvokeDirect(directTranslator, Geo.Column("area").Intersects(point)),
                "Contains(Polygon)" => () => InvokeDirect(directTranslator, Geo.Column("area").Contains(polygon)),
                "Within(MultiPolygon)" => () => InvokeDirect(directTranslator, Geo.Column("area").Within(multiPolygon)),
                "Distance(Point)" => () => InvokeDirect(directTranslator, Geo.Column("area").Distance(point)),
                "Buffer(250)" => () => InvokeDirect(directTranslator, Geo.Column("area").Buffer(250)),
                _ => throw new NotSupportedException(expressionName)
            });
        }
    }

    private static AdaptadorGEO.Sql.SqlFragment InvokeDirect(object translator, AdaptadorGEO.Spatial.GeoExpression expression)
    {
        return translator switch
        {
            MySqlSpatialTranslator mySql => mySql.Translate(expression),
            SqlServerSpatialTranslator sqlServer => sqlServer.Translate(expression),
            PostgreSqlSpatialTranslator postgreSql => postgreSql.Translate(expression),
            _ => throw new NotSupportedException($"Unsupported translator type: {translator.GetType().Name}")
        };
    }
}

#pragma warning disable CS8764
internal sealed class MySqlConnectionStub : DbConnection
{
    public override string? ConnectionString { get; set; } = string.Empty;
    public override string Database => "Geo";
    public override string DataSource => "localhost";
    public override string ServerVersion => "8.0";
    public override ConnectionState State => ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
}

internal sealed class SqlServerConnectionStub : DbConnection
{
    public override string? ConnectionString { get; set; } = string.Empty;
    public override string Database => "Geo";
    public override string DataSource => "localhost";
    public override string ServerVersion => "16.0";
    public override ConnectionState State => ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
}

internal sealed class NpgsqlConnectionStub : DbConnection
{
    public override string? ConnectionString { get; set; } = string.Empty;
    public override string Database => "Geo";
    public override string DataSource => "localhost";
    public override string ServerVersion => "16.0";
    public override ConnectionState State => ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
    protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
}
#pragma warning restore CS8764
