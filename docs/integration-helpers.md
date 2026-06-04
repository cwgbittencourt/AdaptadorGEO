# Helpers de Integração

`AdaptadorGEO.Integration` é uma camada fina para aplicações que querem executar SQL espacial com Dapper ou EF Core sem duplicar o trabalho de montagem dos parâmetros.

A forma preferencial de uso é sempre começar por `GeoDatabase` ou por `AsGeoDatabase()`. Os helpers abaixo apenas adaptam o `SqlFragment` já produzido pela fachada.

## Dapper

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;
using AdaptadorGEO.Integration.Dapper;

using var connection = /* sua IDbConnection */;

var geo = connection.AsGeoDatabase();
SqlFragment fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));
var rows = connection.QuerySqlFragment<MyRow>(fragment);
```

O helper:

- normaliza os nomes dos parâmetros para o Dapper
- encaminha `CommandText`
- passa os valores gerados sem alteração

## EF Core

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration.EntityFrameworkCore;

var geo = dbContext.Database.AsGeoDatabase();
SqlFragment fragment = geo.Translate(
    Geo.Column("area").Contains(Geo.Point(-23.55052, -46.63331)));

await dbContext.Database.ExecuteSqlFragmentAsync(fragment);
```

O helper:

- cria `DbParameter` a partir de `SqlFragment.Parameters`
- vincula `DBNull.Value` para valores nulos
- executa o SQL através de `DatabaseFacade`

## Fronteira

Esses helpers não traduzem geometrias por conta própria. Eles apenas adaptam `SqlFragment` já traduzido para a camada de execução escolhida pela aplicação.
