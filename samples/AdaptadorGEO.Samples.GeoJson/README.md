# AdaptadorGEO.Samples.GeoJson

Este sample mostra como trabalhar com GeoJSON usando a fachada pública `GeoFormats`.

## O que ele demonstra

- converter `GeoGeometry` para GeoJSON com `ToGeoJson(...)`
- ler GeoJSON de volta para os tipos internos com `FromGeoJson(...)`
- ler `Feature` preservando `properties`
- ler `FeatureCollection`
- transformar `FeatureCollection` em `GeometryCollection` pela fachada simples

## Como executar

Execute a partir da raiz do repositório:

```powershell
dotnet run --project samples/AdaptadorGEO.Samples.GeoJson/AdaptadorGEO.Samples.GeoJson.csproj
```

## Observação

O sample é apenas demonstrativo. Ele não depende de banco de dados, ORM ou frontend.
