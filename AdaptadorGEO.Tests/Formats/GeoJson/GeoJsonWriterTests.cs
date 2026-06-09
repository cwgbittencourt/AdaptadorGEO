using System.Text.Json;
using AdaptadorGEO;
using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Formats.GeoJson;

[TestClass]
public class GeoJsonWriterTests
{
    [TestMethod]
    public void Write_Point_ShouldGenerateLongitudeLatitudeCoordinates()
    {
        var point = new GeoPoint(-23.55052, -46.63331);

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(point);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("Point", document.RootElement.GetProperty("type").GetString());
        AssertPosition(document.RootElement.GetProperty("coordinates"), -46.63331, -23.55052);
    }

    [TestMethod]
    public void Write_LineString_ShouldGenerateCoordinates()
    {
        var line = new LineString(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(line);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("LineString", document.RootElement.GetProperty("type").GetString());
        var coordinates = document.RootElement.GetProperty("coordinates");
        Assert.AreEqual(2, coordinates.GetArrayLength());
        AssertPosition(coordinates[0], -46.63, -23.55);
        AssertPosition(coordinates[1], -46.64, -23.56);
    }

    [TestMethod]
    public void Write_Polygon_ShouldGenerateNestedCoordinates()
    {
        var polygon = new Polygon(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.55, -46.63)
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(polygon);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("Polygon", document.RootElement.GetProperty("type").GetString());
        var rings = document.RootElement.GetProperty("coordinates");
        Assert.AreEqual(1, rings.GetArrayLength());
        var ring = rings[0];
        Assert.AreEqual(4, ring.GetArrayLength());
        AssertPosition(ring[0], -46.63, -23.55);
        AssertPosition(ring[1], -46.64, -23.56);
        AssertPosition(ring[2], -46.65, -23.57);
        AssertPosition(ring[3], -46.63, -23.55);
    }

    [TestMethod]
    public void Write_MultiPoint_ShouldGenerateCoordinates()
    {
        var multiPoint = new MultiPoint(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(multiPoint);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("MultiPoint", document.RootElement.GetProperty("type").GetString());
        var coordinates = document.RootElement.GetProperty("coordinates");
        Assert.AreEqual(2, coordinates.GetArrayLength());
        AssertPosition(coordinates[0], -46.63, -23.55);
        AssertPosition(coordinates[1], -46.64, -23.56);
    }

    [TestMethod]
    public void Write_MultiLineString_ShouldGenerateCoordinates()
    {
        var multiLineString = new MultiLineString(new[]
        {
            new LineString(new[]
            {
                new GeoPoint(-23.55, -46.63),
                new GeoPoint(-23.56, -46.64)
            }),
            new LineString(new[]
            {
                new GeoPoint(-23.57, -46.65),
                new GeoPoint(-23.58, -46.66)
            })
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(multiLineString);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("MultiLineString", document.RootElement.GetProperty("type").GetString());
        var coordinates = document.RootElement.GetProperty("coordinates");
        Assert.AreEqual(2, coordinates.GetArrayLength());
        Assert.AreEqual(2, coordinates[0].GetArrayLength());
        Assert.AreEqual(2, coordinates[1].GetArrayLength());
        AssertPosition(coordinates[0][0], -46.63, -23.55);
        AssertPosition(coordinates[0][1], -46.64, -23.56);
        AssertPosition(coordinates[1][0], -46.65, -23.57);
        AssertPosition(coordinates[1][1], -46.66, -23.58);
    }

    [TestMethod]
    public void Write_MultiPolygon_ShouldGenerateCoordinates()
    {
        var multiPolygon = new MultiPolygon(new[]
        {
            new Polygon(new[]
            {
                new GeoPoint(-23.55, -46.63),
                new GeoPoint(-23.56, -46.64),
                new GeoPoint(-23.57, -46.65),
                new GeoPoint(-23.55, -46.63)
            })
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(multiPolygon);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("MultiPolygon", document.RootElement.GetProperty("type").GetString());
        var coordinates = document.RootElement.GetProperty("coordinates");
        Assert.AreEqual(1, coordinates.GetArrayLength());
        Assert.AreEqual(1, coordinates[0].GetArrayLength());
        Assert.AreEqual(4, coordinates[0][0].GetArrayLength());
        AssertPosition(coordinates[0][0][0], -46.63, -23.55);
    }

    [TestMethod]
    public void Write_GeometryCollection_ShouldGenerateNestedGeometries()
    {
        var collection = new GeometryCollection(new GeoGeometry[]
        {
            new GeoPoint(-23.55, -46.63)
        });

        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(collection);
        using var document = JsonDocument.Parse(json);

        Assert.AreEqual("GeometryCollection", document.RootElement.GetProperty("type").GetString());
        var geometries = document.RootElement.GetProperty("geometries");
        Assert.AreEqual(1, geometries.GetArrayLength());
        Assert.AreEqual("Point", geometries[0].GetProperty("type").GetString());
        AssertPosition(geometries[0].GetProperty("coordinates"), -46.63, -23.55);
    }

    private static void AssertPosition(JsonElement coordinates, double longitude, double latitude)
    {
        Assert.AreEqual(JsonValueKind.Array, coordinates.ValueKind);
        Assert.AreEqual(longitude, coordinates[0].GetDouble());
        Assert.AreEqual(latitude, coordinates[1].GetDouble());
    }
}
