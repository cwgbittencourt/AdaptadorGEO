# WKT no AdaptadorGEO

## Objetivo

O `AdaptadorGEO` pode converter entre os tipos geométricos internos e WKT sem depender de banco de dados, ORM ou conexão.

Isso é útil quando a aplicação precisa serializar ou ler geometrias em texto WKT antes de gerar SQL espacial.

## Ordem das coordenadas

O WKT usa a ordem:

```txt
longitude latitude
```

No `AdaptadorGEO`, o ponto interno continua sendo:

```csharp
new GeoPoint(latitude, longitude)
```

Exemplo:

```csharp
var point = new GeoPoint(-23.55052, -46.63331);
var wkt = AdaptadorGEO.Formats.GeoFormats.Render(point);
```

WKT esperado:

```txt
POINT(-46.63331 -23.55052)
```

## Converter GeoGeometry para WKT

Use a fachada pública `GeoFormats`:

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

## Converter WKT para GeoGeometry

O parse também é feito pela fachada pública:

```csharp
using AdaptadorGEO.Geometry;

var wkt = "POINT(-46.63331 -23.55052)";

var geometry = AdaptadorGEO.Formats.GeoFormats.Parse<GeoGeometry>(wkt);
```

## Tipos suportados

O parser aceita:

- `POINT`
- `LINESTRING`
- `POLYGON`
- `MULTIPOINT`
- `MULTILINESTRING`
- `MULTIPOLYGON`
- `GEOMETRYCOLLECTION`

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
