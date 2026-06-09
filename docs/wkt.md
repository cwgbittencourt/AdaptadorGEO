# WKT

O `AdaptadorGEO` agora expõe uma fachada pública para trabalhar com WKT sem depender de banco de dados, ORM ou conexão.

## Fachada pública

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

## Saída WKT

A renderização mantém a ordem `Longitude Latitude` exigida pelo WKT:

- `GeoPoint(-23.55, -46.63)` vira `POINT(-46.63 -23.55)`
- `LineString`, `Polygon`, `MultiPoint`, `MultiLineString`, `MultiPolygon` e `GeometryCollection` seguem o mesmo padrão

## Parser WKT

O parser aceita os seguintes tipos:

- `POINT`
- `LINESTRING`
- `POLYGON`
- `MULTIPOINT`
- `MULTILINESTRING`
- `MULTIPOLYGON`
- `GEOMETRYCOLLECTION`

Exemplo:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Geometry;

var geometry = GeoFormats.Parse<GeoGeometry>("POINT(-46.63331 -23.55052)");
```

## Validações

O parser lança `FormatException` para WKT inválido.

Também preserva as validações do modelo interno:

- `GeoPoint` valida latitude e longitude
- `LineString` exige ao menos dois pontos
- `Polygon` exige anel fechado e ao menos quatro pontos
- `MultiPoint`, `MultiLineString`, `MultiPolygon` e `GeometryCollection` exigem conteúdo

## Limitação atual

O modelo interno de `Polygon` representa apenas um anel externo.

Por isso, o parser aceita apenas polígonos simples, sem buracos.

## Em uma frase

`GeoFormats` cobre o ciclo completo `GeoGeometry -> WKT -> GeoGeometry` com o mesmo modelo geométrico interno usado pelo restante da biblioteca.
