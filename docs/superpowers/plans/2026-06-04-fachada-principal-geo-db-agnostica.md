# Fachada Principal Geo Db-Agnóstica - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** criar uma fachada principal única para a solução Geo, capaz de resolver automaticamente o tradutor correto a partir do provider ativo, sem exigir que o código da aplicação mude quando o banco for MySQL, SQL Server ou PostgreSQL.

**Architecture:** a biblioteca passa a expor um objeto de entrada centrado em provider, mas neutro em termos de implementação concreta de banco. O core continua sem dependência de pacotes de banco específicos: ele recebe metadados do provider via `DbConnection`, `DbProviderFactory` ou uma string de identificação, resolve o tradutor correto e devolve `SqlFragment`. A camada `AdaptadorGEO.Integration` fica responsável por conveniências para `DbConnection` e `DatabaseFacade`, enquanto o core concentra a resolução e a tradução.

**Tech Stack:** .NET 10, `System.Data.Common`, `AdaptadorGEO` core, `AdaptadorGEO.Integration`, MSTest.

---

## File Structure

Antes de implementar, o código precisa ficar dividido por responsabilidade:

- `AdaptadorGEO/Runtime/GeoDatabase.cs` - fachada principal usada pela aplicação para resolver automaticamente o provider e traduzir `GeoExpression` para `SqlFragment`.
- `AdaptadorGEO/Runtime/IGeoTranslatorResolver.cs` - contrato de resolução do tradutor por nome de provider.
- `AdaptadorGEO/Runtime/GeoProviderResolver.cs` - implementação padrão que detecta MySQL, SQL Server e PostgreSQL com base no tipo da conexão ou em identificadores explícitos.
- `AdaptadorGEO/Runtime/GeoProviderKeys.cs` - constantes com os nomes e aliases aceitos para cada provider.
- `AdaptadorGEO.Integration/DbConnectionGeoExtensions.cs` - extensões para criar a fachada a partir de `DbConnection`.
- `AdaptadorGEO.Integration/EntityFrameworkCore/DatabaseFacadeGeoExtensions.cs` - extensões para criar a fachada a partir de `DatabaseFacade`.
- `AdaptadorGEO.Tests/Runtime/GeoDatabaseTests.cs` - testes da resolução automática e dos erros de provider desconhecido.
- `AdaptadorGEO.Tests/Integration/GeoDatabaseExtensionsTests.cs` - testes das extensões de `DbConnection` e EF Core.
- `AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj` - referência adicional para `Microsoft.EntityFrameworkCore.InMemory` caso o teste de `DatabaseFacade` use um provider leve em memória.
- `README.md` e `docs/integration-helpers.md` - documentação da nova entrada única.

## API Proposal

A API principal proposta é esta:

```csharp
using System.Data.Common;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Sql;

public sealed class GeoDatabase
{
    public static GeoDatabase For(DbConnection connection);
    public static GeoDatabase ForProvider(string providerInvariantName);
    public static GeoDatabase For(DbConnection connection, GeoProviderResolver resolver);
    public static GeoDatabase ForProvider(string providerInvariantName, GeoProviderResolver resolver);

    public string ProviderName { get; }

    public SqlFragment Translate(GeoExpression expression);
}
```

Uso esperado:

```csharp
var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));
```

O comportamento desejado é:

- a aplicação cria a fachada uma vez;
- a biblioteca identifica automaticamente o provider ativo;
- a tradução usa o dialeto espacial correto;
- nenhum `if`/`switch` por banco aparece no código de domínio da aplicação.

---

### Task 1: Definir a fachada principal e o resolvedor de provider

**Files:**
- Create: `AdaptadorGEO/Runtime/GeoDatabase.cs`
- Create: `AdaptadorGEO/Runtime/IGeoTranslatorResolver.cs`
- Create: `AdaptadorGEO/Runtime/GeoProviderResolver.cs`
- Create: `AdaptadorGEO/Runtime/GeoProviderKeys.cs`
- Test: `AdaptadorGEO.Tests/Runtime/GeoDatabaseTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Data.Common;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Runtime;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Runtime;

[TestClass]
public sealed class GeoDatabaseTests
{
    [TestMethod]
    public void For_ShouldResolve_MySqlTranslator_FromConnectionTypeName()
    {
        var connection = new FakeMySqlConnection();

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
        Assert.IsTrue(fragment.CommandText.Contains("STWithin", StringComparison.Ordinal));
    }

    [TestMethod]
    public void For_ShouldThrow_ClearMessage_WhenProviderIsUnknown()
    {
        var connection = new FakeUnknownConnection();

        var ex = Assert.ThrowsException<NotSupportedException>(() => GeoDatabase.For(connection));

        StringAssert.Contains(ex.Message, "MySQL");
        StringAssert.Contains(ex.Message, "SQL Server");
        StringAssert.Contains(ex.Message, "PostgreSQL");
    }

    private sealed class FakeMySqlConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = "";
        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "8.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private sealed class FakeUnknownConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = "";
        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoDatabaseTests`

Expected: fail because `GeoDatabase`, `IGeoTranslatorResolver` and the provider resolver do not exist yet.

- [ ] **Step 3: Implement the minimal facade and resolver**

```csharp
public interface IGeoTranslatorResolver
{
    IGeoTranslator Resolve(string providerName);
}

public sealed class GeoProviderResolver : IGeoTranslatorResolver
{
    public static GeoProviderResolver Default { get; } = new();

    public void RegisterAlias(string alias, string providerName) { ... }
    public string NormalizeProviderName(string providerName) { ... }
    public string ResolveProviderName(DbConnection connection) { ... }
    public IGeoTranslator Resolve(string providerName) { ... }
}

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
        var providerName = GeoProviderResolver.Default.ResolveProviderName(connection);
        var translator = GeoProviderResolver.Default.Resolve(providerName);

        return new GeoDatabase(providerName, translator);
    }

    public static GeoDatabase ForProvider(string providerInvariantName)
    {
        var providerName = GeoProviderResolver.Default.NormalizeProviderName(providerInvariantName);
        var translator = GeoProviderResolver.Default.Resolve(providerName);

        return new GeoDatabase(providerName, translator);
    }

    public static GeoDatabase For(DbConnection connection, GeoProviderResolver resolver)
    {
        var providerName = resolver.ResolveProviderName(connection);
        var translator = resolver.Resolve(providerName);
        return new GeoDatabase(providerName, translator);
    }

    public static GeoDatabase ForProvider(string providerInvariantName, GeoProviderResolver resolver)
    {
        var providerName = resolver.NormalizeProviderName(providerInvariantName);
        var translator = resolver.Resolve(providerName);
        return new GeoDatabase(providerName, translator);
    }

    public SqlFragment Translate(GeoExpression expression) => _translator.Translate(expression);
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoDatabaseTests`

Expected: all tests in `GeoDatabaseTests` pass.

- [ ] **Step 5: Commit**

```bash
git add AdaptadorGEO/Runtime/GeoDatabase.cs AdaptadorGEO/Runtime/IGeoTranslatorResolver.cs AdaptadorGEO/Runtime/GeoProviderResolver.cs AdaptadorGEO/Runtime/GeoProviderKeys.cs AdaptadorGEO.Tests/Runtime/GeoDatabaseTests.cs
git commit -m "feat: add provider-agnostic geo facade"
```

### Task 2: Adicionar extensões de conveniência para `DbConnection` e EF Core

**Files:**
- Create: `AdaptadorGEO.Integration/DbConnectionGeoExtensions.cs`
- Create: `AdaptadorGEO.Integration/EntityFrameworkCore/DatabaseFacadeGeoExtensions.cs`
- Modify: `AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj`
- Test: `AdaptadorGEO.Tests/Integration/GeoDatabaseExtensionsTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Data.Common;
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
        var connection = new FakeSqlServerConnection();

        var geo = connection.AsGeoDatabase();

        Assert.AreEqual("SQL Server", geo.ProviderName);
    }

    [TestMethod]
    public void AsGeoDatabase_ShouldCreateFacade_FromDatabaseFacade()
    {
        var context = new FakeDbContext();

        var geo = context.Database.AsGeoDatabase();

        Assert.AreEqual("PostgreSQL", geo.ProviderName);
    }

    private sealed class FakeSqlServerConnection : DbConnection
    {
        public override string ConnectionString { get; set; } = "";
        public override string Database => "geo";
        public override string DataSource => "localhost";
        public override string ServerVersion => "16.0";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    private sealed class FakeDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase("geo-tests");
        }
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoDatabaseExtensionsTests`

Expected: fail because the extensions do not exist yet.

- [ ] **Step 3: Implement the extension methods**

```csharp
public static class DbConnectionGeoExtensions
{
    public static GeoDatabase AsGeoDatabase(this DbConnection connection) => GeoDatabase.For(connection);
}

public static class DatabaseFacadeGeoExtensions
{
    public static GeoDatabase AsGeoDatabase(this DatabaseFacade databaseFacade)
        => GeoDatabase.For(databaseFacade.GetDbConnection());
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoDatabaseExtensionsTests`

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add AdaptadorGEO.Integration/DbConnectionGeoExtensions.cs AdaptadorGEO.Integration/EntityFrameworkCore/DatabaseFacadeGeoExtensions.cs AdaptadorGEO.Tests/Integration/GeoDatabaseExtensionsTests.cs
git commit -m "feat: add geo facade integration extensions"
```

### Task 3: Permitir aliases explícitos e fallback controlado para providers desconhecidos

**Files:**
- Modify: `AdaptadorGEO/Runtime/GeoProviderResolver.cs`
- Modify: `AdaptadorGEO/Runtime/GeoProviderKeys.cs`
- Test: `AdaptadorGEO.Tests/Runtime/GeoProviderResolverTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using AdaptadorGEO.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Runtime;

[TestClass]
public sealed class GeoProviderResolverTests
{
    [TestMethod]
    public void NormalizeProviderName_ShouldAccept_CommonAliases()
    {
        Assert.AreEqual("MySQL", GeoProviderResolver.Default.NormalizeProviderName("MySqlConnector"));
        Assert.AreEqual("SQL Server", GeoProviderResolver.Default.NormalizeProviderName("Microsoft.Data.SqlClient"));
        Assert.AreEqual("PostgreSQL", GeoProviderResolver.Default.NormalizeProviderName("Npgsql"));
    }

    [TestMethod]
    public void ResolveTranslator_ShouldUse_CustomAlias_WhenProvided()
    {
        var resolver = GeoProviderResolver.Default;
        resolver.RegisterAlias("MyCompany.MySql", "MySQL");

        var translator = resolver.Resolve("MyCompany.MySql");

        Assert.IsNotNull(translator);
    }
}
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoProviderResolverTests`

Expected: fail because alias registration and normalization are not implemented yet.

- [ ] **Step 3: Implement alias normalization and custom registration**

```csharp
public sealed class GeoProviderResolver : IGeoTranslatorResolver
{
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MySqlConnector"] = GeoProviderKeys.MySql,
        ["MySql.Data.MySqlClient"] = GeoProviderKeys.MySql,
        ["Microsoft.Data.SqlClient"] = GeoProviderKeys.SqlServer,
        ["System.Data.SqlClient"] = GeoProviderKeys.SqlServer,
        ["Npgsql"] = GeoProviderKeys.PostgreSql
    };

    public void RegisterAlias(string alias, string providerName) => _aliases[alias] = providerName;

    public string NormalizeProviderName(string providerName) { ... }
    public string ResolveProviderName(DbConnection connection) { ... }

    public IGeoTranslator Resolve(string providerName) { ... }
}
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoProviderResolverTests`

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add AdaptadorGEO/Runtime/GeoProviderResolver.cs AdaptadorGEO/Runtime/GeoProviderKeys.cs AdaptadorGEO.Tests/Runtime/GeoProviderResolverTests.cs
git commit -m "feat: support provider aliases in geo resolver"
```

### Task 4: Atualizar a documentação para a nova fachada principal

**Files:**
- Modify: `README.md`
- Modify: `docs/integration-helpers.md`
- Modify: `docs/spatial-sql-usage.md`

- [ ] **Step 1: Update the README opening section**

Add a new example that shows the single entrypoint:

```csharp
var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(
    Geo.Column("area").Contains(Geo.Point(-23.55052, -46.63331)));
```

Explain that the application no longer branches by provider in domain code.

- [ ] **Step 2: Update the integration docs**

Add a dedicated section describing:

- `DbConnection.AsGeoDatabase()`
- `DatabaseFacade.AsGeoDatabase()`
- `GeoDatabase.ForProvider(...)`
- what happens when the provider is unknown

- [ ] **Step 3: Review the examples for consistency**

Make sure the docs use the same provider names as the code:

- `MySQL`
- `SQL Server`
- `PostgreSQL`

- [ ] **Step 4: Commit**

```bash
git add README.md docs/integration-helpers.md docs/spatial-sql-usage.md
git commit -m "docs: describe provider-agnostic geo facade"
```

---

## Self-Review

### 1. Spec coverage

The plan covers the request end to end:

- a single principal facade exists for the application;
- the facade resolves the correct provider automatically;
- the core remains vendor-agnostic and does not depend on MySQL, SQL Server or PostgreSQL packages;
- integration helpers keep the application code unchanged when the database changes;
- documentation is updated to show the new usage pattern.

### 2. Placeholder scan

Checked for placeholders such as `TBD`, `TODO`, or vague steps. None were left in the plan.

### 3. Type consistency

The plan uses one consistent vocabulary:

- `GeoDatabase` as the main facade;
- `IGeoTranslatorResolver` and `GeoProviderResolver` for provider resolution;
- `SqlFragment` as the output;
- `GeoExpression` as the input.

If a different naming choice is preferred, it should be changed consistently across Tasks 1 through 4 before implementation starts.
