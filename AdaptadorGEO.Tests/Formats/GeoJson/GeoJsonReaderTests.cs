using System.Text.Json;
using AdaptadorGEO;
using AdaptadorGEO.Formats.GeoJson;
using AdaptadorGEO.Geometry;
using GeoJsonApi = AdaptadorGEO.Formats.GeoJson.GeoJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Formats.GeoJson;

[TestClass]
public class GeoJsonReaderTests
{
    [TestMethod]
    public void Read_Point_ShouldCreateGeoPoint()
    {
        var json = """
        {
          "type": "Point",
          "coordinates": [-46.63331, -23.55052]
        }
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(GeoPoint));
        var point = (GeoPoint)geometry;
        Assert.AreEqual(-23.55052, point.Latitude);
        Assert.AreEqual(-46.63331, point.Longitude);
    }

    [TestMethod]
    public void Read_LineString_ShouldCreateLineString()
    {
        var json = """
        {
          "type": "LineString",
          "coordinates": [
            [-46.63, -23.55],
            [-46.64, -23.56]
          ]
        }
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(LineString));
        var lineString = (LineString)geometry;
        Assert.AreEqual(2, lineString.Points.Count);
        Assert.AreEqual(-23.55, lineString.Points[0].Latitude);
        Assert.AreEqual(-46.63, lineString.Points[0].Longitude);
    }

    [TestMethod]
    public void Read_Polygon_ShouldCreatePolygon()
    {
        var json = """
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
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(Polygon));
        var polygon = (Polygon)geometry;
        Assert.AreEqual(4, polygon.OuterRing.Count);
        Assert.AreEqual(-23.55, polygon.OuterRing[0].Latitude);
        Assert.AreEqual(-46.63, polygon.OuterRing[0].Longitude);
    }

    [TestMethod]
    public void Read_MultiPoint_ShouldCreateMultiPoint()
    {
        var json = """
        {
          "type": "MultiPoint",
          "coordinates": [
            [-46.63, -23.55],
            [-46.64, -23.56]
          ]
        }
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(MultiPoint));
        var multiPoint = (MultiPoint)geometry;
        Assert.AreEqual(2, multiPoint.Points.Count);
    }

    [TestMethod]
    public void Read_MultiLineString_ShouldCreateMultiLineString()
    {
        var json = """
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
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(MultiLineString));
        var multiLineString = (MultiLineString)geometry;
        Assert.AreEqual(2, multiLineString.LineStrings.Count);
    }

    [TestMethod]
    public void Read_MultiPolygon_ShouldCreateMultiPolygon()
    {
        var json = """
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
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(MultiPolygon));
        var multiPolygon = (MultiPolygon)geometry;
        Assert.AreEqual(1, multiPolygon.Polygons.Count);
    }

    [TestMethod]
    public void Read_GeometryCollection_ShouldCreateGeometryCollection()
    {
        var json = """
        {
          "type": "GeometryCollection",
          "geometries": [
            {
              "type": "Point",
              "coordinates": [-46.63331, -23.55052]
            }
          ]
        }
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(GeometryCollection));
        var collection = (GeometryCollection)geometry;
        Assert.AreEqual(1, collection.Geometries.Count);
        Assert.IsInstanceOfType<GeoPoint>(collection.Geometries[0]);
    }

    [TestMethod]
    public void Read_Feature_ShouldPreserveProperties()
    {
        var json = """
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
        """;

        var feature = GeoJsonApi.ReadFeature(json);

        Assert.IsInstanceOfType(feature.Geometry, typeof(GeoPoint));
        Assert.AreEqual("Garagem Centro", GetStringValue(feature.Properties["nome"]));
        Assert.IsTrue(GetBoolValue(feature.Properties["ativo"]));
    }

    [TestMethod]
    public void Read_FeatureCollection_ShouldPreserveFeatures()
    {
        var json = """
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
        """;

        var collection = GeoJsonApi.ReadFeatureCollection(json);

        Assert.AreEqual(1, collection.Features.Count);
        Assert.IsInstanceOfType(collection.Features[0].Geometry, typeof(GeoPoint));
        Assert.AreEqual("Garagem Centro", GetStringValue(collection.Features[0].Properties["nome"]));
    }

    [TestMethod]
    public void Read_FeatureCollection_AsGeometry_ShouldReturnGeometryCollection()
    {
        var json = """
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
        """;

        var geometry = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        Assert.IsInstanceOfType(geometry, typeof(GeometryCollection));
        var collection = (GeometryCollection)geometry;
        Assert.AreEqual(1, collection.Geometries.Count);
        Assert.IsInstanceOfType(collection.Geometries[0], typeof(GeoPoint));
    }

    private static string? GetStringValue(object? value) =>
        value switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
            _ => value?.ToString()
        };

    private static bool GetBoolValue(object? value) =>
        value switch
        {
            JsonElement element when element.ValueKind == JsonValueKind.True => true,
            JsonElement element when element.ValueKind == JsonValueKind.False => false,
            bool boolValue => boolValue,
            _ => throw new AssertFailedException("Expected a boolean value.")
        };
}
