using AdaptadorGEO.Formats.GeoJson;
using AdaptadorGEO.Geometry;
using GeoJsonFormats = AdaptadorGEO.Formats.GeoFormats;
using GeoJsonApi = AdaptadorGEO.Formats.GeoJson.GeoJson;

namespace AdaptadorGEO.Samples.GeoJson;

internal static class Program
{
    public static void Main()
    {
        var polygon = new Polygon(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.55, -46.63)
        });

        var geoJson = GeoJsonFormats.ToGeoJson(polygon);
        var parsedGeometry = GeoJsonFormats.FromGeoJson(geoJson);

        var feature = GeoJsonApi.ReadFeature("""
        {
          "type": "Feature",
          "geometry": {
            "type": "Point",
            "coordinates": [-46.63331, -23.55052]
          },
          "properties": {
            "nome": "Garagem Centro",
            "ativo": true
          }
        }
        """);

        var featureCollection = GeoJsonApi.ReadFeatureCollection("""
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

        var collectionGeometry = GeoJsonFormats.FromGeoJson("""
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

        Console.WriteLine("GeoJSON sample");
        Console.WriteLine("GeoJSON from polygon:");
        Console.WriteLine(geoJson);
        Console.WriteLine("Parsed geometry type:");
        Console.WriteLine(parsedGeometry.GetType().Name);
        Console.WriteLine("Feature geometry type:");
        Console.WriteLine(feature.Geometry.GetType().Name);
        Console.WriteLine("Feature properties:");
        foreach (var property in feature.Properties)
        {
            Console.WriteLine($"- {property.Key} = {property.Value}");
        }
        Console.WriteLine("FeatureCollection count:");
        Console.WriteLine(featureCollection.Features.Count);
        Console.WriteLine("FeatureCollection as geometry type:");
        Console.WriteLine(collectionGeometry.GetType().Name);
    }
}
