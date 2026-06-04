# Adaptador GEO Spatial SQL Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a .NET class library that models geographic inputs and translates spatial operations into SQL for MySQL, SQL Server, and PostgreSQL so the database performs the spatial processing.

**Architecture:** The library will not execute database calls and will not compute spatial results in memory. Instead, it will expose a small geometry model for inputs such as `Point`, `LineString`, and `Polygon`, plus a provider-neutral operation tree for spatial predicates and transforms. Provider packages will translate that tree into native SQL for each database, using `ST_*` functions where available and SQL Server spatial methods where required, so the consuming application can execute the generated SQL through its own ORM or ADO.NET stack.

**Tech Stack:** .NET 10, C#, xUnit, provider-specific SQL translators, optional ADO.NET-friendly SQL fragment model, `SRID 4326` as the default geographic coordinate reference.

---

## File Structure

The solution is currently minimal, so this plan introduces a focused layout instead of forcing everything into a single project file.

- `AdaptadorGEO/AdaptadorGEO.csproj` - main class library project.
- `AdaptadorGEO/Geometry/` - geographic value objects and validation.
- `AdaptadorGEO/Spatial/` - provider-neutral spatial operation tree.
- `AdaptadorGEO/Sql/` - SQL fragment and parameter model used by translators.
- `AdaptadorGEO/Providers/MySql/` - MySQL SQL translator.
- `AdaptadorGEO/Providers/SqlServer/` - SQL Server spatial translator.
- `AdaptadorGEO/Providers/PostgreSql/` - PostgreSQL/PostGIS translator.
- `AdaptadorGEO.Tests/` - xUnit tests for geometry validation and SQL translation.
- `docs/superpowers/plans/2026-06-03-adaptador-geo-spatial-sql.md` - this implementation plan.

---

### Task 1: Scaffold the solution for library + tests

**Files:**
- Modify: `AdaptadorGEO.slnx`
- Modify: `AdaptadorGEO/AdaptadorGEO.csproj`
- Create: `AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj`
- Create: `AdaptadorGEO.Tests/Usings.cs`

- [ ] **Step 1: Write the failing test project reference check**

```csharp
using Xunit;
using AdaptadorGEO.Geometry;

public class ProjectWiringTests
{
    [Fact]
    public void TestProjectCanReferenceLibrary()
    {
        var point = new GeoPoint(0, 0);

        Assert.Equal(0, point.Latitude);
        Assert.Equal(0, point.Longitude);
    }
}
```

- [ ] **Step 2: Run the test to verify the scaffold is incomplete**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal`

Expected: FAIL because the test project and reference are not wired up yet.

- [ ] **Step 3: Add the minimal project structure**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\AdaptadorGEO\AdaptadorGEO.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

</Project>
```

- [ ] **Step 4: Run the test again to verify the scaffold passes**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal`

Expected: PASS.

- [ ] **Step 5: Commit the scaffold**

```bash
git add AdaptadorGEO.slnx AdaptadorGEO/AdaptadorGEO.csproj AdaptadorGEO.Tests
git commit -m "feat: scaffold spatial sql library"
```

### Task 2: Implement core geographic value objects and validation

**Files:**
- Create: `AdaptadorGEO/Geometry/GeoPoint.cs`
- Create: `AdaptadorGEO/Geometry/LineString.cs`
- Create: `AdaptadorGEO/Geometry/Polygon.cs`
- Create: `AdaptadorGEO/Geometry/CoordinateRange.cs`
- Create: `AdaptadorGEO.Tests/Geometry/GeoPointTests.cs`
- Create: `AdaptadorGEO.Tests/Geometry/LineStringTests.cs`
- Create: `AdaptadorGEO.Tests/Geometry/PolygonTests.cs`

- [ ] **Step 1: Write the failing tests for coordinate validation**

```csharp
using AdaptadorGEO.Geometry;

public class GeoPointTests
{
    [Fact]
    public void Constructor_rejects_latitude_outside_range()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(91, 10));
        Assert.Equal("latitude", ex.ParamName);
    }

    [Fact]
    public void Constructor_rejects_longitude_outside_range()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(10, 181));
        Assert.Equal("longitude", ex.ParamName);
    }

    [Fact]
    public void Point_preserves_latitude_and_longitude_order()
    {
        var point = new GeoPoint(-23.55052, -46.63331);
        Assert.Equal(-23.55052, point.Latitude);
        Assert.Equal(-46.63331, point.Longitude);
    }
}
```

- [ ] **Step 2: Run the tests to verify the geometry types do not exist yet**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoPointTests`

Expected: FAIL because `GeoPoint` is not implemented.

- [ ] **Step 3: Implement the minimal geometry model**

```csharp
namespace AdaptadorGEO.Geometry;

public sealed record GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        CoordinateRange.ValidateLatitude(latitude);
        CoordinateRange.ValidateLongitude(longitude);

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}
```

```csharp
namespace AdaptadorGEO.Geometry;

public static class CoordinateRange
{
    public static void ValidateLatitude(double latitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }
    }

    public static void ValidateLongitude(double longitude)
    {
        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }
}
```

```csharp
namespace AdaptadorGEO.Geometry;

public sealed record LineString
{
    public LineString(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("A LineString must contain at least two points.", nameof(points));
        }

        Points = points.ToArray();
    }

    public IReadOnlyList<GeoPoint> Points { get; }
}
```

```csharp
namespace AdaptadorGEO.Geometry;

public sealed record Polygon
{
    public Polygon(IReadOnlyList<GeoPoint> outerRing)
    {
        if (outerRing.Count < 4)
        {
            throw new ArgumentException("A Polygon outer ring must contain at least four points.", nameof(outerRing));
        }

        if (!outerRing[0].Equals(outerRing[^1]))
        {
            throw new ArgumentException("A Polygon outer ring must be closed.", nameof(outerRing));
        }

        OuterRing = outerRing.ToArray();
    }

    public IReadOnlyList<GeoPoint> OuterRing { get; }
}
```

- [ ] **Step 4: Run the tests to verify validation passes**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter "GeoPointTests|LineStringTests|PolygonTests"`

Expected: PASS.

- [ ] **Step 5: Commit the geometry primitives**

```bash
git add AdaptadorGEO/Geometry AdaptadorGEO.Tests/Geometry
git commit -m "feat: add spatial value objects"
```

### Task 3: Define the provider-neutral spatial operation tree

**Files:**
- Create: `AdaptadorGEO/Spatial/GeoExpression.cs`
- Create: `AdaptadorGEO/Spatial/GeoColumn.cs`
- Create: `AdaptadorGEO/Spatial/GeoLiteral.cs`
- Create: `AdaptadorGEO/Spatial/GeoBufferExpression.cs`
- Create: `AdaptadorGEO/Spatial/GeoContainsExpression.cs`
- Create: `AdaptadorGEO/Spatial/GeoIntersectsExpression.cs`
- Create: `AdaptadorGEO/Spatial/GeoDistanceExpression.cs`
- Create: `AdaptadorGEO/Spatial/GeoWithinExpression.cs`
- Create: `AdaptadorGEO/Sql/SqlFragment.cs`
- Create: `AdaptadorGEO/Sql/SqlParameter.cs`
- Create: `AdaptadorGEO/Spatial/IGeoTranslator.cs`
- Create: `AdaptadorGEO.Tests/Spatial/GeoExpressionTests.cs`

- [ ] **Step 1: Write the failing translator tests**

```csharp
using Xunit;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;

public class GeoExpressionTests
{
    [Fact]
    public void Buffer_expression_exposes_input_geometry_and_distance()
    {
        var expr = new GeoBufferExpression(new GeoColumn("Location"), 250);

        Assert.Equal(250, expr.DistanceMeters);
        Assert.IsType<GeoColumn>(expr.Source);
    }

    [Fact]
    public void Intersects_expression_keeps_both_operands()
    {
        var left = new GeoColumn("Area");
        var right = new GeoLiteral(new GeoPoint(-23.55, -46.63));
        var expr = new GeoIntersectsExpression(left, right);

        Assert.Same(left, expr.Left);
        Assert.Same(right, expr.Right);
    }
}
```

- [ ] **Step 2: Run the tests to verify the operation tree is missing**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoExpressionTests`

Expected: FAIL because the spatial AST types are not implemented.

- [ ] **Step 3: Implement the spatial AST and SQL fragment model**

```csharp
namespace AdaptadorGEO.Sql;

public sealed record SqlParameter(string Name, object? Value);
```

```csharp
namespace AdaptadorGEO.Sql;

public sealed record SqlFragment(string CommandText, IReadOnlyList<SqlParameter> Parameters);
```

```csharp
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;

namespace AdaptadorGEO.Spatial;

public abstract record GeoExpression;

public sealed record GeoColumn(string Name) : GeoExpression;

public sealed record GeoLiteral(GeoPoint Value) : GeoExpression;

public sealed record GeoBufferExpression(GeoExpression Source, double DistanceMeters) : GeoExpression;

public sealed record GeoContainsExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;

public sealed record GeoIntersectsExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;

public sealed record GeoDistanceExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;

public sealed record GeoWithinExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;

public interface IGeoTranslator
{
    SqlFragment Translate(GeoExpression expression);
}
```

- [ ] **Step 4: Run the tests to verify the AST passes**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter GeoExpressionTests`

Expected: PASS.

- [ ] **Step 5: Commit the AST layer**

```bash
git add AdaptadorGEO/Spatial AdaptadorGEO/Sql AdaptadorGEO.Tests/Spatial
git commit -m "feat: add spatial expression tree"
```

### Task 4: Implement MySQL, SQL Server, and PostgreSQL translators

**Files:**
- Create: `AdaptadorGEO/Providers/MySql/MySqlSpatialTranslator.cs`
- Create: `AdaptadorGEO/Providers/SqlServer/SqlServerSpatialTranslator.cs`
- Create: `AdaptadorGEO/Providers/PostgreSql/PostgreSqlSpatialTranslator.cs`
- Create: `AdaptadorGEO/Providers/SpatialDialect.cs`
- Create: `AdaptadorGEO.Tests/Providers/MySqlSpatialTranslatorTests.cs`
- Create: `AdaptadorGEO.Tests/Providers/SqlServerSpatialTranslatorTests.cs`
- Create: `AdaptadorGEO.Tests/Providers/PostgreSqlSpatialTranslatorTests.cs`

- [ ] **Step 1: Write the failing provider translation tests**

```csharp
using Xunit;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Providers.MySql;

public class MySqlSpatialTranslatorTests
{
    [Fact]
    public void Buffer_translates_to_mysql_st_buffer()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoBufferExpression(new GeoColumn("Location"), 250));

        Assert.Contains("ST_Buffer", sql.CommandText);
        Assert.Contains("@p0", sql.CommandText);
    }
}
```

```csharp
using Xunit;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Providers.SqlServer;

public class SqlServerSpatialTranslatorTests
{
    [Fact]
    public void Point_literal_translates_to_sqlserver_geometry_or_geography_constructor()
    {
        IGeoTranslator translator = new SqlServerSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(new GeoPoint(-23.55052, -46.63331)));

        Assert.Contains("STGeomFromText", sql.CommandText);
        Assert.Contains("4326", sql.CommandText);
    }
}
```

```csharp
using Xunit;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Providers.PostgreSql;

public class PostgreSqlSpatialTranslatorTests
{
    [Fact]
    public void Intersects_translates_to_postgis_st_intersects()
    {
        IGeoTranslator translator = new PostgreSqlSpatialTranslator();
        var sql = translator.Translate(
            new GeoIntersectsExpression(
                new GeoColumn("Area"),
                new GeoLiteral(new GeoPoint(-23.55052, -46.63331))));

        Assert.Contains("ST_Intersects", sql.CommandText);
    }
}
```

- [ ] **Step 2: Run the tests to verify provider translators are missing**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter "MySqlSpatialTranslatorTests|SqlServerSpatialTranslatorTests|PostgreSqlSpatialTranslatorTests"`

Expected: FAIL because the provider translators do not exist yet.

- [ ] **Step 3: Implement the provider-specific translators**

```csharp
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Providers.MySql;

public sealed class MySqlSpatialTranslator : IGeoTranslator
{
    public SqlFragment Translate(GeoExpression expression)
    {
        return expression switch
        {
            GeoBufferExpression buffer => new SqlFragment(
                $"ST_Buffer({Render(buffer.Source)}, @p0)",
                new[] { new SqlParameter("@p0", buffer.DistanceMeters) }),
            GeoIntersectsExpression intersects => new SqlFragment(
                $"ST_Intersects({Render(intersects.Left)}, {Render(intersects.Right)})",
                Array.Empty<SqlParameter>()),
            GeoContainsExpression contains => new SqlFragment(
                $"ST_Contains({Render(contains.Left)}, {Render(contains.Right)})",
                Array.Empty<SqlParameter>()),
            GeoDistanceExpression distance => new SqlFragment(
                $"ST_Distance({Render(distance.Left)}, {Render(distance.Right)})",
                Array.Empty<SqlParameter>()),
            GeoWithinExpression within => new SqlFragment(
                $"ST_Within({Render(within.Left)}, {Render(within.Right)})",
                Array.Empty<SqlParameter>()),
            GeoLiteral literal => new SqlFragment(
                "ST_GeomFromText(@p0, 4326)",
                new[] { new SqlParameter("@p0", $"POINT({literal.Value.Longitude} {literal.Value.Latitude})") }),
            GeoColumn column => new SqlFragment($"`{column.Name}`", Array.Empty<SqlParameter>()),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}")
        };
    }

    private static string Render(GeoExpression expression) => expression switch
    {
        GeoColumn column => $"`{column.Name}`",
        GeoLiteral literal => $"ST_GeomFromText('POINT({literal.Value.Longitude} {literal.Value.Latitude})', 4326)",
        _ => throw new NotSupportedException($"Unsupported nested expression: {expression.GetType().Name}")
    };
}
```

```csharp
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Providers.SqlServer;

public sealed class SqlServerSpatialTranslator : IGeoTranslator
{
    public SqlFragment Translate(GeoExpression expression)
    {
        return expression switch
        {
            GeoBufferExpression buffer => new SqlFragment(
                $"{Render(buffer.Source)}.STBuffer(@p0)",
                new[] { new SqlParameter("@p0", buffer.DistanceMeters) }),
            GeoIntersectsExpression intersects => new SqlFragment(
                $"{Render(intersects.Left)}.STIntersects({Render(intersects.Right)})",
                Array.Empty<SqlParameter>()),
            GeoContainsExpression contains => new SqlFragment(
                $"{Render(contains.Left)}.STContains({Render(contains.Right)})",
                Array.Empty<SqlParameter>()),
            GeoDistanceExpression distance => new SqlFragment(
                $"{Render(distance.Left)}.STDistance({Render(distance.Right)})",
                Array.Empty<SqlParameter>()),
            GeoWithinExpression within => new SqlFragment(
                $"{Render(within.Left)}.STWithin({Render(within.Right)})",
                Array.Empty<SqlParameter>()),
            GeoLiteral literal => new SqlFragment(
                "geography::STGeomFromText(@p0, 4326)",
                new[] { new SqlParameter("@p0", $"POINT({literal.Value.Longitude} {literal.Value.Latitude})") }),
            GeoColumn column => new SqlFragment($"[{column.Name}]", Array.Empty<SqlParameter>()),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}")
        };
    }

    private static string Render(GeoExpression expression) => expression switch
    {
        GeoColumn column => $"[{column.Name}]",
        GeoLiteral literal => $"geography::STGeomFromText('POINT({literal.Value.Longitude} {literal.Value.Latitude})', 4326)",
        _ => throw new NotSupportedException($"Unsupported nested expression: {expression.GetType().Name}")
    };
}
```

```csharp
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Providers.PostgreSql;

public sealed class PostgreSqlSpatialTranslator : IGeoTranslator
{
    public SqlFragment Translate(GeoExpression expression)
    {
        return expression switch
        {
            GeoBufferExpression buffer => new SqlFragment(
                $"ST_Buffer({Render(buffer.Source)}, @p0)",
                new[] { new SqlParameter("@p0", buffer.DistanceMeters) }),
            GeoIntersectsExpression intersects => new SqlFragment(
                $"ST_Intersects({Render(intersects.Left)}, {Render(intersects.Right)})",
                Array.Empty<SqlParameter>()),
            GeoContainsExpression contains => new SqlFragment(
                $"ST_Contains({Render(contains.Left)}, {Render(contains.Right)})",
                Array.Empty<SqlParameter>()),
            GeoDistanceExpression distance => new SqlFragment(
                $"ST_Distance({Render(distance.Left)}, {Render(distance.Right)})",
                Array.Empty<SqlParameter>()),
            GeoWithinExpression within => new SqlFragment(
                $"ST_Within({Render(within.Left)}, {Render(within.Right)})",
                Array.Empty<SqlParameter>()),
            GeoLiteral literal => new SqlFragment(
                "ST_GeomFromText(@p0, 4326)",
                new[] { new SqlParameter("@p0", $"POINT({literal.Value.Longitude} {literal.Value.Latitude})") }),
            GeoColumn column => new SqlFragment($"\"{column.Name}\"", Array.Empty<SqlParameter>()),
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}")
        };
    }

    private static string Render(GeoExpression expression) => expression switch
    {
        GeoColumn column => $"\"{column.Name}\"",
        GeoLiteral literal => $"ST_GeomFromText('POINT({literal.Value.Longitude} {literal.Value.Latitude})', 4326)",
        _ => throw new NotSupportedException($"Unsupported nested expression: {expression.GetType().Name}")
    };
}
```

- [ ] **Step 4: Run the tests to verify each dialect passes**

Run: `dotnet test AdaptadorGEO.Tests/AdaptadorGEO.Tests.csproj -v minimal --filter "MySqlSpatialTranslatorTests|SqlServerSpatialTranslatorTests|PostgreSqlSpatialTranslatorTests"`

Expected: PASS.

- [ ] **Step 5: Commit the provider translators**

```bash
git add AdaptadorGEO/Providers AdaptadorGEO.Tests/Providers
git commit -m "feat: add provider spatial translators"
```

### Task 5: Document the consumption pattern and provider contract

**Files:**
- Create: `docs/spatial-sql-usage.md`
- Create: `docs/spatial-provider-contract.md`
- Modify: `README.md`

- [ ] **Step 1: Write the failing documentation check**

```text
The repository must document:
1. How a consuming application selects a translator.
2. How the application passes a column and a geometry literal into a spatial expression.
3. How the generated SQL is executed by the app's own data layer.
```

- [ ] **Step 2: Run a docs review pass and verify the usage guidance is absent**

Run: `Get-ChildItem docs -Recurse`

Expected: The new usage docs are not present yet.

- [ ] **Step 3: Add the usage docs and README examples**

```markdown
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using AdaptadorGEO.Providers.PostgreSql;

IGeoTranslator translator = new PostgreSqlSpatialTranslator();
var expression = new GeoIntersectsExpression(
    new GeoColumn("boundary"),
    new GeoLiteral(new GeoPoint(-23.55052, -46.63331)));

SqlFragment sql = translator.Translate(expression);
```

```markdown
The library does not connect to the database.
The consuming application executes `sql.CommandText` with `sql.Parameters` through its own provider, ORM, or ADO.NET pipeline.
```

- [ ] **Step 4: Run a final verification that the docs exist**

Run: `Get-ChildItem docs -Recurse | Select-Object FullName`

Expected: `docs/spatial-sql-usage.md` and `docs/spatial-provider-contract.md` are listed.

- [ ] **Step 5: Commit the documentation**

```bash
git add README.md docs/spatial-sql-usage.md docs/spatial-provider-contract.md
git commit -m "docs: describe spatial sql usage"
```

---

## Self-Review

### 1. Spec coverage

- The plan covers geographic primitives required by the user: `Point`, `LineString`, and `Buffer` as an operation that returns a translated spatial expression.
- The plan explicitly adds `Polygon`, which is necessary because buffer operations and spatial predicates commonly return or consume polygonal shapes.
- The plan keeps the library out of memory-only processing and makes SQL translation the core behavior.
- The plan supports all three databases the user named: MySQL, SQL Server, and PostgreSQL.
- The plan keeps database execution in the consuming application, which matches the clarified requirement that the library should not own the connection/provider.

### 2. Placeholder scan

- No `TBD`, `TODO`, or vague “add appropriate handling” text remains in the plan.
- Every task has explicit file paths, test commands, and a commit step.

### 3. Type consistency

- `GeoPoint`, `LineString`, `Polygon`, `GeoExpression`, `GeoBufferExpression`, `GeoIntersectsExpression`, `GeoContainsExpression`, `GeoDistanceExpression`, `GeoWithinExpression`, `SqlFragment`, `SqlParameter`, and `IGeoTranslator` are used consistently across tasks.
- The plan uses `Latitude` and `Longitude` in that order for the public geometry model, while the translators are responsible for emitting the database-specific coordinate order required by each provider.

### 4. Scope check

- The plan is narrow enough for one implementation cycle.
- If later requirements demand LINQ expression-tree rewriting or direct EF Core provider hooks, that should be split into a separate plan instead of folding it into this one.
