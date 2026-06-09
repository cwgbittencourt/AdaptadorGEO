# GeoJSON no AdaptadorGEO

## Objetivo

O `AdaptadorGEO` pode converter entre os tipos geométricos internos e GeoJSON sem depender de banco de dados, ORM ou frontend.

Isso é útil quando a aplicação recebe geometrias desenhadas em mapas web, como Leaflet ou OpenLayers, e precisa transformar esse payload em tipos internos antes de gerar SQL espacial.

## Ordem das coordenadas

O GeoJSON usa a ordem:

```txt
[longitude, latitude]
```

No `AdaptadorGEO`, o ponto interno continua sendo:

```csharp
new GeoPoint(latitude, longitude)
```

Exemplo:

```csharp
var point = new GeoPoint(-23.55052, -46.63331);
var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(point);
```

GeoJSON esperado:

```json
{
  "type": "Point",
  "coordinates": [-46.63331, -23.55052]
}
```

## Converter GeoGeometry para GeoJSON

Use a fachada pública `GeoFormats`:

```csharp
using AdaptadorGEO.Geometry;

var geometry = new Polygon(new[]
{
    new GeoPoint(-23.55, -46.63),
    new GeoPoint(-23.56, -46.64),
    new GeoPoint(-23.57, -46.65),
    new GeoPoint(-23.55, -46.63)
});

var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(geometry);
```

## Converter GeoJSON para GeoGeometry

O parse também é feito pela fachada pública:

```csharp
using AdaptadorGEO.Geometry;

var json = """
{
  "type": "Point",
  "coordinates": [-46.63331, -23.55052]
}
""";

var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);
```

## Feature

`Feature` preserva a geometria e um dicionário de propriedades:

```csharp
using AdaptadorGEO.Formats.GeoJson;

var feature = GeoJson.ReadFeature("""
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
""");

var geometry = feature.Geometry;
var properties = feature.Properties;
```

As `properties` não são misturadas com `GeoGeometry`.

## FeatureCollection

`FeatureCollection` preserva a lista de features:

```csharp
using AdaptadorGEO.Formats.GeoJson;

var collection = GeoJson.ReadFeatureCollection("""
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
""");

var features = collection.Features;
```

Se você usar a fachada simples `GeoFormats.FromGeoJson(...)` com um `FeatureCollection`, o resultado será uma `GeometryCollection` com as geometrias das features.

## Integração com API ASP.NET Core

Exemplo conceitual de uso em uma API:

```csharp
public sealed class CriarAreaDto
{
    public string Nome { get; set; } = string.Empty;

    public string GeoJson { get; set; } = string.Empty;
}

[HttpPost("areas")]
public IActionResult CriarArea([FromBody] CriarAreaDto dto)
{
    var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(dto.GeoJson);

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

O `AdaptadorGEO` não cria controllers nem depende de ASP.NET Core.

## Integração com mapas web

Exemplo conceitual do lado do frontend:

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

Esse exemplo é apenas documental. O projeto `AdaptadorGEO` não depende de Leaflet ou OpenLayers.

## Limitações

- O modelo interno continua sendo 2D.
- `Polygon` representa apenas o anel externo.
- GeoJSON com buracos em polígonos não é suportado.
- A ordem das coordenadas deve ser sempre `[longitude, latitude]`.

## Boas práticas

- Valide o GeoJSON na borda da API.
- Converta para tipos internos antes de gerar SQL espacial.
- Use `Feature` e `FeatureCollection` quando precisar preservar propriedades adicionais.

## Em uma frase

`GeoFormats` cobre o ciclo `GeoGeometry -> GeoJSON -> GeoGeometry` e deixa a aplicação decidir como usar o resultado no restante do fluxo espacial.
