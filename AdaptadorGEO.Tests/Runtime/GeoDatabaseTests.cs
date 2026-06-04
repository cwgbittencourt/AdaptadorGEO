using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Runtime;

[TestClass]
public sealed class GeoDatabaseTests
{
    [TestMethod]
    public void For_ShouldResolve_MySqlTranslator_FromConnectionTypeName()
    {
        var connection = new MySqlConnectionStub();

        var geo = GeoDatabase.For(connection);
        var fragment = geo.Translate(Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));

        Assert.AreEqual("MySQL", geo.ProviderName);
        Assert.AreEqual("ST_Intersects(`area`, ST_GeomFromText(@p0, 4326))", fragment.CommandText);
    }

    [TestMethod]
    public void ForProvider_ShouldResolve_PostgreSqlTranslator_FromInvariantName()
    {
        var geo = GeoDatabase.ForProvider("Npgsql");

        var fragment = geo.Translate(Geo.Column("area").Within(Geo.Point(-23.55052, -46.63331)));

        Assert.AreEqual("PostgreSQL", geo.ProviderName);
        Assert.IsTrue(fragment.CommandText.Contains("ST_Within", StringComparison.Ordinal));
    }

    [TestMethod]
    public void For_ShouldThrow_ClearMessage_WhenProviderIsUnknown()
    {
        var connection = new UnknownConnectionStub();

        var ex = Assert.ThrowsException<NotSupportedException>(() => GeoDatabase.For(connection));

        StringAssert.Contains(ex.Message, "MySQL");
        StringAssert.Contains(ex.Message, "SQL Server");
        StringAssert.Contains(ex.Message, "PostgreSQL");
    }

    private sealed class MySqlConnectionStub : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "8.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string? databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private sealed class UnknownConnectionStub : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string? databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
