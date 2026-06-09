# Implementacao_Geojson.md

# Tarefa: Implementar suporte GeoJSON no AdaptadorGEO

## 1. Contexto

O repositório `AdaptadorGEO` é uma biblioteca .NET para representar geometrias, montar expressões espaciais e traduzi-las para SQL nativo de PostgreSQL/PostGIS, MySQL e SQL Server.

O projeto principal já possui tipos geométricos na pasta:

```txt
AdaptadorGEO/Geometry
```

Tipos existentes considerados neste plano:

```txt
GeoGeometry
GeoPoint
LineString
Polygon
MultiPoint
MultiLineString
MultiPolygon
GeometryCollection
GeoWkt
```

O arquivo `GeoWkt.cs` já existe e trata a renderização de geometrias para WKT. Portanto, **não implementar WKT nesta tarefa**.

Esta tarefa é exclusivamente para adicionar suporte a **GeoJSON**.

---

## 2. Objetivo

Adicionar suporte nativo a GeoJSON dentro do projeto `AdaptadorGEO`, permitindo converter:

```txt
GeoGeometry -> GeoJSON
GeoJSON -> GeoGeometry
```

O objetivo é permitir que aplicações web com mapas, como Leaflet ou OpenLayers, enviem GeoJSON para uma API, e o `AdaptadorGEO` consiga converter esse GeoJSON para seus tipos internos.

Fluxo esperado:

```txt
Frontend / Mapa
    ↓
GeoJSON
    ↓
AdaptadorGEO
    ↓
GeoGeometry
    ↓
GeoDatabase.Translate(...)
    ↓
SQL espacial por provider
```

---

## 3. Regras obrigatórias

1. Não alterar a responsabilidade principal do `AdaptadorGEO`.
2. Não abrir conexão com banco.
3. Não executar SQL.
4. Não depender de EF Core.
5. Não depender de Dapper.
6. Não depender de Leaflet.
7. Não depender de OpenLayers.
8. Não criar regra de negócio da aplicação consumidora.
9. Usar preferencialmente `System.Text.Json`.
10. Manter a ordem correta das coordenadas GeoJSON: `[longitude, latitude]`.
11. Preservar os tipos internos atuais do projeto.
12. Criar testes unitários.
13. Criar documentação em Markdown.
14. Não implementar WKT nesta tarefa.

---

## 4. Estrutura de arquivos sugerida

Criar a seguinte estrutura no projeto principal:

```txt
AdaptadorGEO/Formats
AdaptadorGEO/Formats/GeoJson
```

Arquivos sugeridos:

```txt
AdaptadorGEO/Formats/GeoJson/GeoJsonWriter.cs
AdaptadorGEO/Formats/GeoJson/GeoJsonReader.cs
AdaptadorGEO/Formats/GeoJson/GeoJsonOptions.cs
AdaptadorGEO/Formats/GeoJson/GeoJsonFeature.cs
AdaptadorGEO/Formats/GeoJson/GeoJsonFeatureCollection.cs
AdaptadorGEO/Formats/GeoJson/GeoJsonException.cs
AdaptadorGEO/Formats/GeoFormats.cs
```

Caso o projeto já tenha algum padrão diferente de organização, seguir o padrão existente, mas manter a separação conceitual entre:

```txt
Geometry = tipos geométricos
Formats = conversores de formato
```

---

## 5. API pública esperada

Criar uma fachada pública simples:

```csharp
using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats;

public static class GeoFormats
{
    public static string ToGeoJson(GeoGeometry geometry)
    {
        return GeoJsonWriter.Write(geometry);
    }

    public static GeoGeometry FromGeoJson(string geoJson)
    {
        return GeoJsonReader.ReadGeometry(geoJson);
    }
}
```

Também criar uma classe pública específica para GeoJSON:

```csharp
using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

public static class GeoJson
{
    public static string Write(GeoGeometry geometry, GeoJsonOptions? options = null);

    public static GeoGeometry ReadGeometry(string geoJson);

    public static GeoJsonFeature ReadFeature(string geoJson);

    public static GeoJsonFeatureCollection ReadFeatureCollection(string geoJson);
}
```

Uso esperado:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Formats;

var point = new GeoPoint(-23.55052, -46.63331);

var geoJson = GeoFormats.ToGeoJson(point);

var geometry = GeoFormats.FromGeoJson(geoJson);

var fragment = GeoDatabase
    .ForProvider("Npgsql")
    .Translate(Geo.Column("area").Intersects(geometry));
```

---

## 6. Ordem das coordenadas

No GeoJSON, a ordem correta é:

```txt
[longitude, latitude]
```

No `AdaptadorGEO`, o ponto é representado como:

```csharp
new GeoPoint(latitude, longitude)
```

Exemplo:

```csharp
var point = new GeoPoint(-23.55052, -46.63331);
```

GeoJSON esperado:

```json
{
  "type": "Point",
  "coordinates": [-46.63331, -23.55052]
}
```

Regra obrigatória:

```txt
GeoPoint.Longitude -> coordinates[0]
GeoPoint.Latitude  -> coordinates[1]
```

---

## 7. Tipos GeoJSON que devem ser suportados

O writer deve converter os tipos internos para GeoJSON:

```txt
GeoPoint           -> Point
LineString         -> LineString
Polygon            -> Polygon
MultiPoint         -> MultiPoint
MultiLineString    -> MultiLineString
MultiPolygon       -> MultiPolygon
GeometryCollection -> GeometryCollection
```

O reader deve converter GeoJSON para tipos internos:

```txt
Point              -> GeoPoint
LineString         -> LineString
Polygon            -> Polygon
MultiPoint         -> MultiPoint
MultiLineString    -> MultiLineString
MultiPolygon       -> MultiPolygon
GeometryCollection -> GeometryCollection
Feature            -> GeoJsonFeature
FeatureCollection  -> GeoJsonFeatureCollection
```

---

## 8. Implementação do GeoJsonWriter

Criar:

```csharp
namespace AdaptadorGEO.Formats.GeoJson;

internal static class GeoJsonWriter
{
    public static string Write(GeoGeometry geometry, GeoJsonOptions? options = null)
    {
        // implementar
    }
}
```

O writer deve gerar JSON válido para:

### Point

Entrada:

```csharp
new GeoPoint(-23.55052, -46.63331)
```

Saída:

```json
{
  "type": "Point",
  "coordinates": [-46.63331, -23.55052]
}
```

### LineString

Saída esperada:

```json
{
  "type": "LineString",
  "coordinates": [
    [-46.63, -23.55],
    [-46.64, -23.56]
  ]
}
```

### Polygon

Saída esperada:

```json
{
  "type": "Polygon",
  "coordinates": [
    [
      [-46.63, -23.55],
      [-46.64, -23.56],
      [-46.65, -23.57],
      [-46.63, -23.55]
    ]
  ]
}
```

### MultiPoint

```json
{
  "type": "MultiPoint",
  "coordinates": [
    [-46.63, -23.55],
    [-46.64, -23.56]
  ]
}
```

### MultiLineString

```json
{
  "type": "MultiLineString",
  "coordinates": [
    [
      [-46.63, -23.55],
      [-46.64, -23.56]
    ],
    [
      [-46.65, -23.57],
      [-46.66, -23.58]
    ]
  ]
}
```

### MultiPolygon

```json
{
  "type": "MultiPolygon",
  "coordinates": [
    [
      [
        [-46.63, -23.55],
        [-46.64, -23.56],
        [-46.65, -23.57],
        [-46.63, -23.55]
      ]
    ]
  ]
}
```

### GeometryCollection

```json
{
  "type": "GeometryCollection",
  "geometries": [
    {
      "type": "Point",
      "coordinates": [-46.63331, -23.55052]
    }
  ]
}
```

---

## 9. Implementação do GeoJsonReader

Criar:

```csharp
namespace AdaptadorGEO.Formats.GeoJson;

internal static class GeoJsonReader
{
    public static GeoGeometry ReadGeometry(string geoJson)
    {
        // implementar
    }
}
```

O reader deve aceitar:

1. Geometry pura.
2. Feature.
3. FeatureCollection.

### Geometry pura

Exemplo:

```json
{
  "type": "Point",
  "coordinates": [-46.63331, -23.55052]
}
```

Deve retornar:

```csharp
GeoPoint
```

### Feature

Exemplo:

```json
{
  "type": "Feature",
  "geometry": {
    "type": "Point",
    "coordinates": [-46.63331, -23.55052]
  },
  "properties": {
    "nome": "Garagem Centro"
  }
}
```

Deve ser possível:

```csharp
var feature = GeoJson.ReadFeature(json);

var geometry = feature.Geometry;
var properties = feature.Properties;
```

### FeatureCollection

Exemplo:

```json
{
  "type": "FeatureCollection",
  "features": [
    {
      "type": "Feature",
      "geometry": {
        "type": "Point",
        "coordinates": [-46.63331, -23.55052]
      },
      "properties": {
        "nome": "Garagem Centro"
      }
    }
  ]
}
```

Deve ser possível:

```csharp
var collection = GeoJson.ReadFeatureCollection(json);

var features = collection.Features;
```

Para a fachada simples:

```csharp
GeoFormats.FromGeoJson(json)
```

se receber `FeatureCollection`, pode retornar uma `GeometryCollection` com as geometrias das features.

---

## 10. Classes para Feature e FeatureCollection

Criar:

```csharp
using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonFeature
{
    public GeoGeometry Geometry { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }

    public GeoJsonFeature(
        GeoGeometry geometry,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Properties = properties ?? new Dictionary<string, object?>();
    }
}
```

Criar:

```csharp
namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonFeatureCollection
{
    public IReadOnlyList<GeoJsonFeature> Features { get; }

    public GeoJsonFeatureCollection(IReadOnlyList<GeoJsonFeature> features)
    {
        Features = features ?? throw new ArgumentNullException(nameof(features));
    }
}
```

As `properties` não devem ser misturadas com `GeoGeometry`.

---

## 11. GeoJsonOptions

Criar:

```csharp
namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonOptions
{
    public bool Indented { get; init; }

    public int? Srid { get; init; } = 4326;

    public bool IncludeCrs { get; init; } = false;
}
```

Observação:

Por padrão:

```csharp
IncludeCrs = false
```

O SRID deve ser tratado pela aplicação ou pelo banco quando necessário.

---

## 12. GeoJsonException

Criar uma exceção específica:

```csharp
namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonException : Exception
{
    public GeoJsonException(string message)
        : base(message)
    {
    }

    public GeoJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

Usar essa exceção para erros de parsing e validação.

---

## 13. Validações obrigatórias

O reader deve validar:

1. GeoJSON nulo.
2. GeoJSON vazio.
3. JSON inválido.
4. Campo `type` ausente.
5. Campo `coordinates` ausente quando o tipo exigir.
6. Tipo GeoJSON não suportado.
7. Coordenadas não numéricas.
8. `Point` com menos de duas coordenadas.
9. `LineString` com menos de dois pontos.
10. `Polygon` com anel externo com menos de quatro pontos.
11. `Polygon` com anel externo não fechado.
12. `MultiPoint` vazio.
13. `MultiLineString` com linhas inválidas.
14. `MultiPolygon` com polígonos inválidos.
15. `GeometryCollection` sem `geometries`.
16. `Feature` sem `geometry`.
17. `FeatureCollection` sem `features`.

---

## 14. Testes unitários

Criar a pasta:

```txt
AdaptadorGEO.Tests/Formats/GeoJson
```

Arquivos sugeridos:

```txt
GeoJsonWriterTests.cs
GeoJsonReaderTests.cs
GeoJsonRoundTripTests.cs
GeoJsonValidationTests.cs
GeoJsonFeatureTests.cs
```

### 14.1 Testes do Writer

Criar testes para:

```txt
GeoPoint
LineString
Polygon
MultiPoint
MultiLineString
MultiPolygon
GeometryCollection
```

Exemplo:

```csharp
[Fact]
public void Write_Point_ShouldGenerateGeoJsonWithLongitudeLatitude()
{
    var point = new GeoPoint(-23.55052, -46.63331);

    var json = GeoFormats.ToGeoJson(point);

    json.Should().Contain("\"type\":\"Point\"");
    json.Should().Contain("-46.63331");
    json.Should().Contain("-23.55052");
}
```

Ajustar a asserção conforme o padrão de serialização do projeto.

### 14.2 Testes do Reader

Criar testes para:

```txt
Point
LineString
Polygon
MultiPoint
MultiLineString
MultiPolygon
GeometryCollection
Feature
FeatureCollection
```

Exemplo:

```csharp
[Fact]
public void Read_Point_ShouldCreateGeoPoint()
{
    var json = """
    {
      "type": "Point",
      "coordinates": [-46.63331, -23.55052]
    }
    """;

    var geometry = GeoFormats.FromGeoJson(json);

    var point = geometry.Should().BeOfType<GeoPoint>().Subject;
    point.Latitude.Should().Be(-23.55052);
    point.Longitude.Should().Be(-46.63331);
}
```

### 14.3 Testes RoundTrip

Testar:

```txt
GeoGeometry -> GeoJSON -> GeoGeometry
```

Para todos os tipos principais.

Exemplo:

```csharp
[Fact]
public void RoundTrip_Point_ShouldPreserveCoordinates()
{
    var original = new GeoPoint(-23.55052, -46.63331);

    var json = GeoFormats.ToGeoJson(original);

    var parsed = GeoFormats.FromGeoJson(json);

    parsed.Should().BeEquivalentTo(original);
}
```

### 14.4 Testes de validação

Criar testes para erros:

```txt
JSON vazio
JSON inválido
type ausente
coordinates ausente
Point inválido
LineString com um ponto
Polygon aberto
Feature sem geometry
FeatureCollection sem features
```

Exemplo:

```csharp
[Fact]
public void Read_InvalidJson_ShouldThrowGeoJsonException()
{
    var json = "{ invalid json }";

    var action = () => GeoFormats.FromGeoJson(json);

    action.Should().Throw<GeoJsonException>();
}
```

---

## 15. Documentação

Criar:

```txt
docs/geojson.md
```

Conteúdo mínimo:

```md
# GeoJSON no AdaptadorGEO

## Objetivo

## Ordem das coordenadas

## Converter GeoGeometry para GeoJSON

## Converter GeoJSON para GeoGeometry

## Trabalhando com Feature

## Trabalhando com FeatureCollection

## Integração com API ASP.NET Core

## Integração com mapas web

## Limitações

## Boas práticas
```

Atualizar o `README.md` com uma seção curta:

```md
## GeoJSON

O AdaptadorGEO permite converter geometrias internas para GeoJSON e ler GeoJSON para os tipos internos.

```csharp
var point = new GeoPoint(-23.55052, -46.63331);

var json = GeoFormats.ToGeoJson(point);

var geometry = GeoFormats.FromGeoJson(json);
```
```

---

## 16. Exemplo de uso com API ASP.NET Core

Adicionar em `docs/geojson.md` um exemplo parecido com:

```csharp
public sealed class CriarAreaDto
{
    public string Nome { get; set; } = string.Empty;

    public string GeoJson { get; set; } = string.Empty;
}

[HttpPost("areas")]
public IActionResult CriarArea([FromBody] CriarAreaDto dto)
{
    var geometry = GeoFormats.FromGeoJson(dto.GeoJson);

    var fragment = GeoDatabase
        .ForProvider("Npgsql")
        .Translate(Geo.Column("area").Intersects(geometry));

    return Ok(new
    {
        dto.Nome,
        Tipo = geometry.GetType().Name,
        Sql = fragment.CommandText
    });
}
```

Esse exemplo é apenas didático. O `AdaptadorGEO` não deve criar controller nem depender de ASP.NET Core.

---

## 17. Exemplo de uso com Leaflet

Adicionar em `docs/geojson.md` apenas como exemplo conceitual:

```javascript
const geoJson = drawnLayer.toGeoJSON();

await fetch("/api/geo/areas", {
    method: "POST",
    headers: {
        "Content-Type": "application/json"
    },
    body: JSON.stringify({
        nome: "Área desenhada",
        geoJson: JSON.stringify(geoJson.geometry)
    })
});
```

Esse exemplo é apenas documentação. O projeto `AdaptadorGEO` não deve depender de Leaflet.

---

## 18. Critérios de aceite

A implementação estará pronta quando:

1. `GeoFormats.ToGeoJson(GeoGeometry)` funcionar.
2. `GeoFormats.FromGeoJson(string)` funcionar.
3. `GeoJson.Write(...)` funcionar.
4. `GeoJson.ReadGeometry(...)` funcionar.
5. `GeoJson.ReadFeature(...)` funcionar.
6. `GeoJson.ReadFeatureCollection(...)` funcionar.
7. Todos os tipos principais forem suportados.
8. A ordem `[longitude, latitude]` estiver correta.
9. `Feature` preservar `properties`.
10. `FeatureCollection` preservar a lista de features.
11. `FeatureCollection` puder ser convertida para `GeometryCollection` via fachada simples.
12. Testes unitários passarem.
13. `docs/geojson.md` for criado.
14. `README.md` for atualizado.
15. Nenhuma dependência de banco, ORM ou frontend for adicionada.

---

## 19. Checklist para o Codex

Executar nesta ordem:

```txt
[ ] Ler a estrutura atual do projeto.
[ ] Confirmar nomes e construtores reais das classes em AdaptadorGEO/Geometry.
[ ] Confirmar padrão atual dos testes.
[ ] Criar pasta Formats/GeoJson.
[ ] Criar GeoJsonException.
[ ] Criar GeoJsonOptions.
[ ] Criar GeoJsonFeature.
[ ] Criar GeoJsonFeatureCollection.
[ ] Criar GeoJsonWriter.
[ ] Criar GeoJsonReader.
[ ] Criar fachada GeoFormats.
[ ] Criar fachada GeoJson.
[ ] Criar testes do writer.
[ ] Criar testes do reader.
[ ] Criar testes de roundtrip.
[ ] Criar testes de validação.
[ ] Criar docs/geojson.md.
[ ] Atualizar README.md.
[ ] Rodar dotnet test.
[ ] Corrigir falhas.
[ ] Rodar dotnet build.
[ ] Entregar resumo das alterações.
```

---

## 20. Comando de validação

Ao final, executar:

```bash
dotnet build
dotnet test
```

Se houver solução `.slnx`, usar o comando adequado para a solução existente.

---

## 21. Observação final

Esta tarefa deve deixar o `AdaptadorGEO` mais completo como biblioteca geoespacial.

Após esta implementação, o projeto passará a cobrir:

```txt
GeoJSON  <->  GeoGeometry  ->  Expressões espaciais  ->  SQL por provider
```

O WKT já existente deve ser preservado e não deve ser refeito nesta tarefa.
