using AdaptadorGEO.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Runtime;

[TestClass]
public sealed class GeoProviderResolverTests
{
    [TestMethod]
    public void NormalizeProviderName_ShouldAccept_CommonAliases()
    {
        var resolver = GeoProviderResolver.Default;

        Assert.AreEqual("MySQL", resolver.NormalizeProviderName("MySqlConnector"));
        Assert.AreEqual("SQL Server", resolver.NormalizeProviderName("Microsoft.Data.SqlClient"));
        Assert.AreEqual("PostgreSQL", resolver.NormalizeProviderName("Npgsql"));
    }

    [TestMethod]
    public void Resolve_ShouldUse_CustomAlias_WhenProvided()
    {
        var resolver = GeoProviderResolver.Default;
        resolver.RegisterAlias("MyCompany.MySql", "MySQL");

        var translator = resolver.Resolve("MyCompany.MySql");

        Assert.IsNotNull(translator);
    }
}
