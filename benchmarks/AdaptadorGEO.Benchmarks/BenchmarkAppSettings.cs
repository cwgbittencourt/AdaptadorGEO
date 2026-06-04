using System.Text.Json;

namespace AdaptadorGEO.Benchmarks;

internal sealed class BenchmarkAppSettings
{
    public ExecutionOptions Execution { get; set; } = new();

    public ConnectionStringsOptions Connections { get; set; } = new();

    public static BenchmarkAppSettings Load(string path)
    {
        var settings = new BenchmarkAppSettings();

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<BenchmarkAppSettings>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (parsed is not null)
            {
                settings = parsed;
                settings.Execution ??= new ExecutionOptions();
                settings.Connections ??= new ConnectionStringsOptions();
            }
        }

        settings.Execution ??= new ExecutionOptions();
        settings.Connections ??= new ConnectionStringsOptions();
        settings.ApplyEnvironmentOverrides();
        return settings;
    }

    private void ApplyEnvironmentOverrides()
    {
        Connections.MySql ??= Environment.GetEnvironmentVariable("ADAPTORGEO_MYSQL_CONNECTION");
        Connections.SqlServer ??= Environment.GetEnvironmentVariable("ADAPTORGEO_SQLSERVER_CONNECTION");
        Connections.PostgreSql ??= Environment.GetEnvironmentVariable("ADAPTORGEO_POSTGRESQL_CONNECTION");

        Execution.TableName = Environment.GetEnvironmentVariable("ADAPTORGEO_BENCH_TABLE") ?? Execution.TableName;

        if (TryReadInt("ADAPTORGEO_BENCH_SEED_ROWS", out var seedRows))
        {
            Execution.SeedRows = seedRows;
        }

        if (TryReadInt("ADAPTORGEO_BENCH_COMMAND_TIMEOUT", out var timeoutSeconds))
        {
            Execution.CommandTimeoutSeconds = timeoutSeconds;
        }
    }

    private static bool TryReadInt(string name, out int value)
    {
        value = 0;
        var raw = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out value);
    }
}

internal sealed class ExecutionOptions
{
    public string TableName { get; set; } = "geo_features";

    public int SeedRows { get; set; } = 128;

    public int CommandTimeoutSeconds { get; set; } = 30;
}

internal sealed class ConnectionStringsOptions
{
    public string? MySql { get; set; }

    public string? SqlServer { get; set; }

    public string? PostgreSql { get; set; }
}
