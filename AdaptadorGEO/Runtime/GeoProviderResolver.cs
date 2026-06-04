using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using AdaptadorGEO.Spatial;
using System.Data.Common;

namespace AdaptadorGEO.Runtime;

public sealed class GeoProviderResolver : IGeoTranslatorResolver
{
    private readonly Dictionary<string, string> _aliases;

    public GeoProviderResolver()
    {
        _aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MySqlConnector"] = GeoProviderKeys.MySql,
            ["MySql.Data.MySqlClient"] = GeoProviderKeys.MySql,
            ["MySQL"] = GeoProviderKeys.MySql,
            ["Microsoft.Data.SqlClient"] = GeoProviderKeys.SqlServer,
            ["System.Data.SqlClient"] = GeoProviderKeys.SqlServer,
            ["SqlClient"] = GeoProviderKeys.SqlServer,
            ["Npgsql"] = GeoProviderKeys.PostgreSql,
            ["PostgreSql"] = GeoProviderKeys.PostgreSql,
            ["PostgreSQL"] = GeoProviderKeys.PostgreSql
        };
    }

    public static GeoProviderResolver Default => new();

    public void RegisterAlias(string alias, string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        _aliases[alias] = NormalizeProviderName(providerName);
    }

    public string NormalizeProviderName(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (_aliases.TryGetValue(providerName, out var resolved))
        {
            return resolved;
        }

        if (Matches(providerName, "mysql"))
        {
            return GeoProviderKeys.MySql;
        }

        if (Matches(providerName, "sqlserver") || Matches(providerName, "sql client") || Matches(providerName, "sqlclient"))
        {
            return GeoProviderKeys.SqlServer;
        }

        if (Matches(providerName, "npgsql") || Matches(providerName, "postgres"))
        {
            return GeoProviderKeys.PostgreSql;
        }

        if (string.Equals(providerName, GeoProviderKeys.MySql, StringComparison.OrdinalIgnoreCase))
        {
            return GeoProviderKeys.MySql;
        }

        if (string.Equals(providerName, GeoProviderKeys.SqlServer, StringComparison.OrdinalIgnoreCase))
        {
            return GeoProviderKeys.SqlServer;
        }

        if (string.Equals(providerName, GeoProviderKeys.PostgreSql, StringComparison.OrdinalIgnoreCase))
        {
            return GeoProviderKeys.PostgreSql;
        }

        throw new NotSupportedException(
            $"Provider '{providerName}' is not supported. Supported providers are: {GeoProviderKeys.MySql}, {GeoProviderKeys.SqlServer}, {GeoProviderKeys.PostgreSql}.");
    }

    public string ResolveProviderName(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var type = connection.GetType();
        var candidates = new[]
        {
            type.FullName ?? string.Empty,
            type.Name,
            type.Namespace ?? string.Empty,
            type.Assembly.GetName().Name ?? string.Empty
        };

        foreach (var candidate in candidates)
        {
            var mapped = TryResolveCandidate(candidate);
            if (mapped is not null)
            {
                return mapped;
            }
        }

        throw new NotSupportedException(
            $"Unable to resolve the database provider from connection type '{type.FullName ?? type.Name}'. Supported providers are: {GeoProviderKeys.MySql}, {GeoProviderKeys.SqlServer}, {GeoProviderKeys.PostgreSql}.");
    }

    public IGeoTranslator Resolve(string providerName)
    {
        var normalized = NormalizeProviderName(providerName);

        return normalized switch
        {
            GeoProviderKeys.MySql => new MySqlSpatialTranslator(),
            GeoProviderKeys.SqlServer => new SqlServerSpatialTranslator(),
            GeoProviderKeys.PostgreSql => new PostgreSqlSpatialTranslator(),
            _ => throw new NotSupportedException(
                $"Provider '{providerName}' is not supported. Supported providers are: {GeoProviderKeys.MySql}, {GeoProviderKeys.SqlServer}, {GeoProviderKeys.PostgreSql}.")
        };
    }

    private string? TryResolveCandidate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (_aliases.TryGetValue(candidate, out var alias))
        {
            return alias;
        }

        if (Matches(candidate, "mysql"))
        {
            return GeoProviderKeys.MySql;
        }

        if (Matches(candidate, "sqlclient") || Matches(candidate, "sql server") || Matches(candidate, "sqlserver"))
        {
            return GeoProviderKeys.SqlServer;
        }

        if (Matches(candidate, "npgsql") || Matches(candidate, "postgres"))
        {
            return GeoProviderKeys.PostgreSql;
        }

        return null;
    }

    private static bool Matches(string value, string token) =>
        value.Contains(token, StringComparison.OrdinalIgnoreCase);
}
