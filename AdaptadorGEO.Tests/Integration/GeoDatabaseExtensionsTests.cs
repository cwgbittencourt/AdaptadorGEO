using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using AdaptadorGEO.Integration;
using AdaptadorGEO.Integration.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Integration;

[TestClass]
public sealed class GeoDatabaseExtensionsTests
{
    [TestMethod]
    public void AsGeoDatabase_ShouldCreateFacade_FromDbConnection()
    {
        var connection = new SqlServerConnectionStub();

        var geo = connection.AsGeoDatabase();

        Assert.AreEqual("SQL Server", geo.ProviderName);
    }

    [TestMethod]
    public void AsGeoDatabase_ShouldCreateFacade_FromDatabaseFacade()
    {
        using var context = new SqlServerDbContext();

        var geo = context.Database.AsGeoDatabase();

        Assert.AreEqual("SQL Server", geo.ProviderName);
    }

    private sealed class SqlServerDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");
        }
    }

    private sealed class SqlServerConnectionStub : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;

        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string? databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
