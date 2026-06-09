# Como usar o AdaptadorGEO

Este documento explica, em linguagem direta, como usar a biblioteca no dia a dia.

## Ideia principal

O `AdaptadorGEO` serve para montar expressões espaciais em C# e transformá-las em SQL específico de banco de dados.

A forma recomendada de começar é sempre pela fachada `GeoDatabase`.

Em outras palavras:

- você já tem uma conexão ou um `DbContext` na aplicação;
- o `AdaptadorGEO` descobre qual banco está sendo usado;
- a biblioteca gera o SQL correto para esse banco;
- a sua aplicação executa esse SQL.

## O fluxo normal

O fluxo normal tem quatro passos:

1. pegar a conexão já existente na aplicação;
2. criar a fachada `GeoDatabase`;
3. montar a expressão espacial;
4. executar o `SqlFragment` gerado.

Exemplo mental:

- a aplicação informa qual tabela ou coluna espacial quer consultar;
- a biblioteca transforma isso em SQL;
- o banco executa a consulta;
- o resultado volta para a aplicação.

## Quando usar `GeoDatabase`

Use `GeoDatabase` quando quiser que a biblioteca resolva automaticamente se o banco é MySQL, SQL Server ou PostgreSQL.

Esse é o caminho mais simples e mais estável para o uso normal.

Exemplo:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;

using var connection = /* sua DbConnection */;

var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));
```

O que isso significa:

- `GeoDatabase.For(connection)` identifica o provider;
- `Geo.Column("area")` aponta a coluna espacial;
- `Intersects(...)` monta a intenção da consulta;
- `Translate(...)` converte tudo em SQL nativo.

## Quando usar `AsGeoDatabase()`

Se a sua aplicação já trabalha com Dapper ou EF Core, use `AsGeoDatabase()`.

Esse caminho existe para ficar mais curto e mais natural no código da aplicação.

### Com Dapper

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;
using AdaptadorGEO.Integration.Dapper;

using var connection = /* sua IDbConnection */;

var geo = connection.AsGeoDatabase();
var fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));

var rows = connection.QuerySqlFragment<MyRow>(fragment);
```

Nesse fluxo:

- a conexão já existe;
- `AsGeoDatabase()` resolve o provider;
- `QuerySqlFragment` executa o SQL sem você montar parâmetros na mão.

### Com EF Core

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration.EntityFrameworkCore;

var geo = dbContext.Database.AsGeoDatabase();
var fragment = geo.Translate(
    Geo.Column("area").Contains(
        Geo.Polygon(
            Geo.Point(-23.55, -46.63),
            Geo.Point(-23.56, -46.64),
            Geo.Point(-23.57, -46.65),
            Geo.Point(-23.55, -46.63))));

await dbContext.Database.ExecuteSqlFragmentAsync(fragment);
```

Nesse fluxo:

- o `DbContext` já conhece o provider;
- a fachada é criada a partir do `DatabaseFacade`;
- o SQL gerado é enviado para execução pelo EF Core.

## Quando usar os providers diretos

Os tipos como `MySqlSpatialTranslator`, `SqlServerSpatialTranslator` e `PostgreSqlSpatialTranslator` continuam existindo.

Eles não são o caminho normal.

Use esses tradutores quando:

- quiser controlar o provider manualmente;
- estiver escrevendo testes específicos;
- estiver montando um cenário avançado de SQL.

Na prática, para uso normal, prefira:

- `GeoDatabase.For(connection)`
- `connection.AsGeoDatabase()`
- `dbContext.Database.AsGeoDatabase()`

## O que a biblioteca retorna

A tradução sempre gera um `SqlFragment`.

Esse fragmento tem duas partes:

- `CommandText` - o SQL que o banco deve executar;
- `Parameters` - os valores que precisam ser enviados junto.

Isso é importante porque a biblioteca não executa o banco sozinha.

Ela prepara o comando.

Quem executa é a aplicação.

## O que você pode montar

A biblioteca já suporta:

- `Point`
- `LineString`
- `Polygon`
- `MultiPoint`
- `MultiLineString`
- `MultiPolygon`
- `GeometryCollection`

E também suporta operações espaciais como:

- `Buffer`
- `Contains`
- `Intersects`
- `Within`
- `Distance`

## Como pensar no uso

Se você quiser uma regra simples, pense assim:

- se você tem uma conexão: use `GeoDatabase.For(connection)`
- se você está com Dapper: use `connection.AsGeoDatabase()`
- se você está com EF Core: use `dbContext.Database.AsGeoDatabase()`
- se quiser algo avançado: use o tradutor direto do provider
- se precisar de ida e volta entre geometria e texto: use [docs/wkt.md](docs/wkt.md)

## O que a biblioteca não faz

Ela não:

- abre conexão com banco
- executa query sozinha
- faz cálculo geométrico em memória como motor principal
- substitui o seu ORM ou ADO.NET

Ela prepara a tradução espacial.

## Exemplo de persistência

Se a sua aplicação precisa criar ou atualizar uma região no banco, o fluxo continua sendo o mesmo:

- montar a geometria em C#;
- traduzir essa geometria com `GeoDatabase`;
- encaixar o `SqlFragment` no `INSERT` ou `UPDATE`;
- executar o comando com a infraestrutura da aplicação.

### Exemplo com ADO.NET

```csharp
using AdaptadorGEO;
using Microsoft.Data.SqlClient;

using var connection = new SqlConnection("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");
connection.Open();

var geo = GeoDatabase.For(connection);

var areaFragment = geo.Translate(
    Geo.Polygon(
        Geo.Point(-23.55, -46.63),
        Geo.Point(-23.56, -46.64),
        Geo.Point(-23.57, -46.65),
        Geo.Point(-23.55, -46.63)));

using var insert = connection.CreateCommand();
insert.CommandText = $@"
INSERT INTO regions (name, area)
VALUES (@name, {areaFragment.CommandText});";

insert.Parameters.AddWithValue("@name", "Região Central");

foreach (var parameter in areaFragment.Parameters)
{
    insert.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
}

insert.ExecuteNonQuery();
```

Neste exemplo:

- `GeoDatabase.For(connection)` resolve o provider ativo;
- `geo.Translate(...)` gera o trecho espacial no dialeto correto do banco;
- `areaFragment.CommandText` entra no `INSERT`;
- `areaFragment.Parameters` fornece os valores que a aplicação precisa enviar.

### Exemplo de atualização

O mesmo padrão vale para `UPDATE`:

```csharp
using AdaptadorGEO;
using Microsoft.Data.SqlClient;

using var connection = new SqlConnection("Server=localhost;Database=Geo;Trusted_Connection=True;TrustServerCertificate=True");
connection.Open();

var geo = GeoDatabase.For(connection);

var areaFragment = geo.Translate(
    Geo.Polygon(
        Geo.Point(-23.50, -46.60),
        Geo.Point(-23.51, -46.61),
        Geo.Point(-23.52, -46.60),
        Geo.Point(-23.50, -46.60)));

using var update = connection.CreateCommand();
update.CommandText = $@"
UPDATE regions
SET name = @name,
    area = {areaFragment.CommandText}
WHERE id = @id;";

update.Parameters.AddWithValue("@id", 10);
update.Parameters.AddWithValue("@name", "Nova Região");

foreach (var parameter in areaFragment.Parameters)
{
    update.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
}

update.ExecuteNonQuery();
```

Esse modelo deixa a persistência sob responsabilidade da aplicação, enquanto o `AdaptadorGEO` cuida da parte espacial e da tradução para o banco configurado.

Se você também precisa converter a geometria para ou a partir de WKT antes de persistir, veja: [docs/wkt.md](docs/wkt.md).

## Em uma frase

O `AdaptadorGEO` transforma uma intenção espacial escrita em C# em SQL espacial nativo, deixando a aplicação decidir apenas como executar esse SQL.
