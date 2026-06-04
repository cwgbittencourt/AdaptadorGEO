using AdaptadorGEO.Runtime;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Sql;
using System.Data.Common;

namespace AdaptadorGEO;

public sealed class GeoDatabase
{
    private readonly IGeoTranslator _translator;

    private GeoDatabase(string providerName, IGeoTranslator translator)
    {
        ProviderName = providerName;
        _translator = translator;
    }

    public string ProviderName { get; }

    public static GeoDatabase For(DbConnection connection)
    {
        var resolver = GeoProviderResolver.Default;
        return For(connection, resolver);
    }

    public static GeoDatabase ForProvider(string providerInvariantName)
    {
        var resolver = GeoProviderResolver.Default;
        return ForProvider(providerInvariantName, resolver);
    }

    public static GeoDatabase For(DbConnection connection, GeoProviderResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(resolver);

        var providerName = resolver.ResolveProviderName(connection);
        var translator = resolver.Resolve(providerName);

        return new GeoDatabase(providerName, translator);
    }

    public static GeoDatabase ForProvider(string providerInvariantName, GeoProviderResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        var providerName = resolver.NormalizeProviderName(providerInvariantName);
        var translator = resolver.Resolve(providerName);

        return new GeoDatabase(providerName, translator);
    }

    public SqlFragment Translate(GeoExpression expression) => _translator.Translate(expression);
}
