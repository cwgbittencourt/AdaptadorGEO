# AdaptadorGEO

`AdaptadorGEO` é uma biblioteca .NET para montar expressões espaciais e traduzi-las para SQL nativo de MySQL, SQL Server e PostgreSQL/PostGIS.

`GeoDatabase` é a fachada principal da solução. A aplicação deve começar por ela ou por `AsGeoDatabase()` para resolver automaticamente o provider ativo sem mudar o código de domínio.

O pacote principal não abre conexão com banco nem calcula resultados espaciais em memória. Ele retorna objetos `SqlFragment` que a aplicação consumidora executa com seu próprio ORM ou pipeline ADO.NET.

## Conteúdo

- [Pacotes](#pacotes)
- [O que a solução faz](#o-que-a-solução-faz)
- [Fachada principal](#fachada-principal)
- [O que você pode construir](#o-que-você-pode-construir)
- [Tipos geométricos](#tipos-geométricos)
- [API fluente](#api-fluente)
- [Tradução direta por provider](#tradução-direta-por-provider)
- [Helpers de integração](#helpers-de-integração)
- [Performance](#performance)
- [Exemplos](#exemplos)
- [Formato de saída](#formato-de-saída)
- [Restrições](#restrições)
- [WKT](#wkt)
- [GeoJSON](#geojson)
- [Mais detalhes](#mais-detalhes)

## Pacotes

- `AdaptadorGEO` - tipos geométricos, expressões espaciais e tradutores de provider
- `AdaptadorGEO.Integration` - helpers para Dapper e EF Core

## O que a solução faz

`AdaptadorGEO` foi criado para trabalhar com geometrias espaciais sem tirar do banco a responsabilidade de processar a operação.

Com esta solução você pode:

- representar geometrias como `Point`, `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon` e `GeometryCollection`
- montar expressões espaciais como `Buffer`, `Contains`, `Intersects`, `Within` e `Distance`
- resolver automaticamente o tradutor correto com `GeoDatabase` ou `AsGeoDatabase()`
- traduzir essas expressões para SQL nativo de MySQL, SQL Server e PostgreSQL/PostGIS
- receber um `SqlFragment` com `CommandText` e `Parameters`
- executar o fragmento com a infraestrutura da própria aplicação
- usar os helpers de integração para Dapper e EF Core sem duplicar a montagem dos parâmetros

## Fachada principal

A forma recomendada de uso é criar uma única fachada `GeoDatabase` a partir da conexão já existente na aplicação. A partir dela, o tradutor correto é resolvido automaticamente:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;

using var connection = /* sua DbConnection */;

var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));
```

Objeto retornado:

```csharp
GeoDatabase { ProviderName = "SQL Server" }
SqlFragment { ... }
```

Se você quiser montar a fachada explicitamente, também pode usar:

```csharp
var geo = GeoDatabase.ForProvider("Npgsql");
```

Se precisar controlar aliases personalizados de provider, use `GeoProviderResolver`.

Ou, nos projetos que usam os helpers de integração:

```csharp
var geo = connection.AsGeoDatabase();
var geoFromEf = dbContext.Database.AsGeoDatabase();
```

## O que você pode construir

- `Point`
- `LineString`
- `Polygon`
- `MultiPoint`
- `MultiLineString`
- `MultiPolygon`
- `GeometryCollection`
- expressões espaciais:
  - `Buffer`
  - `Contains`
  - `Intersects`
  - `Within`
  - `Distance`

## Tipos geométricos

### Point

```csharp
using AdaptadorGEO.Geometry;

var point = new GeoPoint(-23.55052, -46.63331);
```

Objeto retornado:

```csharp
GeoPoint { Latitude = -23.55052, Longitude = -46.63331 }
```

### LineString

```csharp
var line = new LineString(new[]
{
    new GeoPoint(-23.55, -46.63),
    new GeoPoint(-23.56, -46.64)
});
```

Objeto retornado:

```csharp
LineString
{
    Points = [GeoPoint(-23.55, -46.63), GeoPoint(-23.56, -46.64)]
}
```

### Polygon

```csharp
var polygon = new Polygon(new[]
{
    new GeoPoint(-23.55, -46.63),
    new GeoPoint(-23.56, -46.64),
    new GeoPoint(-23.57, -46.65),
    new GeoPoint(-23.55, -46.63)
});
```

Objeto retornado:

```csharp
Polygon
{
    OuterRing = [GeoPoint(-23.55, -46.63), GeoPoint(-23.56, -46.64), GeoPoint(-23.57, -46.65), GeoPoint(-23.55, -46.63)]
}
```

### MultiPoint, MultiLineString, MultiPolygon, GeometryCollection

```csharp
var multiPoint = Geo.MultiPoint(
    Geo.Point(-23.55, -46.63),
    Geo.Point(-23.56, -46.64));

var multiLine = Geo.MultiLineString(
    Geo.LineString(Geo.Point(-23.55, -46.63), Geo.Point(-23.56, -46.64)));

var multiPolygon = Geo.MultiPolygon(
    Geo.Polygon(
        Geo.Point(-23.55, -46.63),
        Geo.Point(-23.56, -46.64),
        Geo.Point(-23.57, -46.65),
        Geo.Point(-23.55, -46.63)));

var collection = Geo.GeometryCollection(
    Geo.Point(-23.55, -46.63),
    multiPoint,
    multiLine,
    multiPolygon);
```

Objetos retornados:

```csharp
MultiPoint { Points = [...] }
MultiLineString { LineStrings = [...] }
MultiPolygon { Polygons = [...] }
GeometryCollection { Geometries = [...] }
```

## API fluente

Você pode montar expressões com a fachada `Geo` e os métodos de extensão.

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Spatial;

GeoExpression expression =
    Geo.Column("area")
       .Intersects(Geo.Point(-23.55052, -46.63331));
```

Objeto retornado:

```csharp
GeoIntersectsExpression
{
    Left = GeoColumn("area"),
    Right = GeoLiteral(GeoPoint(-23.55052, -46.63331))
}
```

Outras chamadas disponíveis:

```csharp
var buffer = Geo.Column("area").Buffer(250);
var contains = Geo.Column("area").Contains(Geo.Point(-23.55, -46.63));
var within = Geo.Column("area").Within(Geo.MultiPolygon(...));
var distance = Geo.Column("area").Distance(Geo.Point(-23.55, -46.63));
```

Objetos retornados:

- `GeoBufferExpression`
- `GeoContainsExpression`
- `GeoWithinExpression`
- `GeoDistanceExpression`

## Tradução direta por provider

O caminho recomendado é a fachada `GeoDatabase`. Os providers diretos continuam disponíveis apenas para cenários avançados, testes ou composição explícita de SQL.

Cada tradutor retorna um `SqlFragment` com:

- `CommandText` - SQL nativo para o banco de destino
- `Parameters` - valores que devem ser vinculados pela aplicação

Mapeamento disponível:

- MySQL: `MySqlSpatialTranslator`
- SQL Server: `SqlServerSpatialTranslator`
- PostgreSQL/PostGIS: `PostgreSqlSpatialTranslator`

Exemplo de uso avançado:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Providers.PostgreSql;

var fragment = new PostgreSqlSpatialTranslator().Translate(
    Geo.Column("area").Within(
        Geo.MultiPolygon(
            Geo.Polygon(
                Geo.Point(-23.55, -46.63),
                Geo.Point(-23.56, -46.64),
                Geo.Point(-23.57, -46.65),
                Geo.Point(-23.55, -46.63)))));
```

Objeto retornado: `SqlFragment`

## Helpers de integração

`AdaptadorGEO.Integration` adapta `SqlFragment` ao estilo de execução que a sua aplicação já usa.

### Dapper

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;
using AdaptadorGEO.Integration.Dapper;

using var connection = /* sua IDbConnection */;
var geo = connection.AsGeoDatabase();
var fragment = geo.Translate(Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));

var rows = connection.QuerySqlFragment<MyRow>(fragment);
```

Objeto retornado:

```csharp
IEnumerable<MyRow>
```

O helper:

- normaliza os nomes dos parâmetros para o Dapper
- encaminha `CommandText`
- encaminha os valores gerados sem alteração

### EF Core

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

Objeto retornado:

```csharp
Task<int>
```

O helper:

- cria instâncias de `DbParameter` a partir de `SqlFragment.Parameters`
- converte `null` para `DBNull.Value`
- executa o SQL através de `DatabaseFacade`

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

## Performance

`AdaptadorGEO` foi desenhado para ter baixo overhead de tradução.

Na prática, o custo principal das consultas espaciais continua no banco de dados, porque a biblioteca apenas monta a expressão e gera o `SqlFragment`.

O que vale a pena medir:

- tempo de tradução da expressão para `SqlFragment`
- alocações por operação
- consistência entre `GeoDatabase` e os tradutores diretos

O que não deve ser confundido com a performance da biblioteca:

- execução real da query no banco
- latência de rede
- uso de índices espaciais
- custo de `Buffer`, `Distance` e outras operações pesadas no motor do banco

Para comparação entre providers, existem dois caminhos:

- `translation` para medir só a montagem do `SqlFragment`
- `execution` para executar as queries reais em bancos locais subidos com `benchmarks/docker-compose.yml`

O benchmark de tradução continua sendo o mais leve e o primeiro a ser usado. O de execução entra quando você quiser observar o comportamento real dos bancos.

Também existe um baseline comparativo entre frameworks no mesmo banco e na mesma tabela de teste:

| Cenário | AdaptadorGEO | Dapper | EF Core + NetTopologySuite |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,90 ms/op | 0,90 ms/op | 0,94 ms/op |
| `Contains(Polygon)` | 0,83 ms/op | 0,82 ms/op | 0,87 ms/op |
| `Within(MultiPolygon)` | 0,84 ms/op | 0,83 ms/op | 0,87 ms/op |
| `Distance(Point)` | 1,10 ms/op | 1,10 ms/op | 1,18 ms/op |

`Buffer(250)` continua fora desse baseline porque, no modo atual, ele não entra como predicado booleano de `WHERE`.

Relatório completo: [docs/framework-comparison-report-2026-06-04.md](docs/framework-comparison-report-2026-06-04.md)
Relatório final consolidado: [docs/final-validation-report-2026-06-04.md](docs/final-validation-report-2026-06-04.md)

Análise técnica:

- `AdaptadorGEO` usa `DbCommand` direto sobre o `SqlFragment`, com montagem simples de parâmetros.
- `Dapper` adiciona uma camada leve de extensão e binding, então tende a ficar muito próximo.
- `EF Core + NetTopologySuite` passa por `DbContext`, pipeline de LINQ, tradução adicional e infraestrutura de materialização, o que adiciona overhead.
- O ganho medido aqui é pequeno e fica principalmente no lado do cliente; isso não prova, por si só, que o SQL gerado seja diferente ou que o plano de execução seja melhor.
- Para confirmar a origem do ganho, vale comparar o SQL final, o plano de execução e o `STATISTICS IO/TIME` no SQL Server.
- `Buffer(250)` segue fora do baseline porque o cenário atual mede filtros em `WHERE`, e `Buffer` produz geometria.

Se quiser inspecionar o SQL gerado por cada framework, rode o modo de comparação com `--dump-sql`:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --dump-sql --iterations=100 --warmup=10
```

## Por que usar

O valor do `AdaptadorGEO` não é apenas performance. Ele existe para separar a lógica espacial do provider e da infraestrutura de acesso sem perder previsibilidade.

Na prática, ele ajuda quando você quer:

- reutilizar as mesmas expressões espaciais em MySQL, SQL Server e PostgreSQL/PostGIS
- manter o SQL espacial explícito e previsível
- evitar acoplamento forte com um ORM específico para montar consultas espaciais
- integrar com ADO.NET, Dapper ou EF Core sem reescrever a lógica de domínio
- centralizar a construção de `Buffer`, `Contains`, `Intersects`, `Within` e `Distance`

Ele faz mais sentido em projetos que precisam de portabilidade, controle sobre o SQL gerado e uma camada espacial única para a aplicação. Se o projeto já está totalmente fechado em um único provider e sem necessidade de abstração espacial, o ganho tende a ser menor.

## Exemplos

Os exemplos ficam separados da biblioteca principal e não são incluídos no pacote publicado.

- `samples/AdaptadorGEO.Samples.AdoNet`
- `samples/AdaptadorGEO.Samples.Dapper`
- `samples/AdaptadorGEO.Samples.EFCore`

Cada projeto mostra um caminho de uso diferente:

- ADO.NET puro com `DbConnection` e `DbCommand`
- Dapper com `AsGeoDatabase()` e `QuerySqlFragment`
- EF Core com `DatabaseFacade.AsGeoDatabase()`

## Formato de saída

Toda tradução retorna:

```csharp
SqlFragment
{
    CommandText = "... provider SQL ...",
    Parameters = [ ... ]
}
```

Isso significa:

- a biblioteca monta a expressão
- o provider traduz essa expressão
- a aplicação executa o resultado

## Restrições

- SRID é `4326`
- a ordem das coordenadas em WKT é `longitude latitude`
- a biblioteca não consulta banco de dados diretamente
- os helpers de integração não fazem tradução geométrica por conta própria

## WKT

`AdaptadorGEO` também expõe uma fachada pública para trabalhar com WKT sem depender de banco de dados, ORM ou conexão.

Use `GeoFormats` para converter entre geometria interna e WKT:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Geometry;

var polygon = new Polygon(new[]
{
    new GeoPoint(-23.55, -46.63),
    new GeoPoint(-23.56, -46.64),
    new GeoPoint(-23.57, -46.65),
    new GeoPoint(-23.55, -46.63)
});

var wkt = GeoFormats.Render(polygon);
var parsed = GeoFormats.Parse<Polygon>(wkt);
```

O parser aceita:

- `POINT`
- `LINESTRING`
- `POLYGON`
- `MULTIPOINT`
- `MULTILINESTRING`
- `MULTIPOLYGON`
- `GEOMETRYCOLLECTION`

Detalhes completos: [docs/wkt.md](docs/wkt.md)

## GeoJSON

`AdaptadorGEO` também expõe uma fachada pública para converter geometrias internas para GeoJSON e ler GeoJSON de volta para os tipos internos.

Exemplo:

```csharp
using AdaptadorGEO.Geometry;

var point = new GeoPoint(-23.55052, -46.63331);

var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(point);
var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);
```

O parser aceita:

- `Point`
- `LineString`
- `Polygon`
- `MultiPoint`
- `MultiLineString`
- `MultiPolygon`
- `GeometryCollection`
- `Feature`
- `FeatureCollection`

Detalhes completos: [docs/geojson.md](docs/geojson.md)

## Mais detalhes

- Contrato espacial: [docs/spatial-provider-contract.md](docs/spatial-provider-contract.md)
- Uso geral: [docs/spatial-sql-usage.md](docs/spatial-sql-usage.md)
- Helpers de integração: [docs/integration-helpers.md](docs/integration-helpers.md)
- WKT: [docs/wkt.md](docs/wkt.md)
- GeoJSON: [docs/geojson.md](docs/geojson.md)
- Performance benchmark: [docs/performance-benchmark.md](docs/performance-benchmark.md)
- Uso final: [docs/uso-final.md](docs/uso-final.md)
- Samples: [samples/README.md](samples/README.md)
