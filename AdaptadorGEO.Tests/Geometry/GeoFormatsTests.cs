using AdaptadorGEO;
using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class GeoFormatsTests
{
    [TestMethod]
    public void Render_and_parse_geo_point_preserve_longitude_latitude_order()
    {
        var point = new GeoPoint(-23.55052, -46.63331);

        var wkt = GeoFormats.Render(point);
        var parsed = GeoFormats.Parse<GeoPoint>(wkt);

        Assert.AreEqual("POINT(-46.63331 -23.55052)", wkt);
        Assert.AreEqual(point, parsed);
    }

    [TestMethod]
    public void Render_and_parse_linestring_round_trip()
    {
        var line = new LineString(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        });

        var wkt = GeoFormats.Render(line);
        var parsed = GeoFormats.Parse<LineString>(wkt);

        Assert.AreEqual("LINESTRING(-46.63 -23.55, -46.64 -23.56)", wkt);
        Assert.IsInstanceOfType(parsed, typeof(LineString));
        CollectionAssert.AreEqual(line.Points.ToArray(), parsed.Points.ToArray());
    }

    [TestMethod]
    public void Render_and_parse_polygon_round_trip()
    {
        var polygon = new Polygon(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.55, -46.63)
        });

        var wkt = GeoFormats.Render(polygon);
        var parsed = GeoFormats.Parse<Polygon>(wkt);

        Assert.AreEqual("POLYGON((-46.63 -23.55, -46.64 -23.56, -46.65 -23.57, -46.63 -23.55))", wkt);
        Assert.IsInstanceOfType(parsed, typeof(Polygon));
        CollectionAssert.AreEqual(polygon.OuterRing.ToArray(), parsed.OuterRing.ToArray());
    }

    [TestMethod]
    public void Render_and_parse_multipoint_round_trip()
    {
        var multiPoint = new MultiPoint(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        });

        var wkt = GeoFormats.Render(multiPoint);
        var parsed = GeoFormats.Parse<MultiPoint>(wkt);

        Assert.AreEqual("MULTIPOINT((-46.63 -23.55), (-46.64 -23.56))", wkt);
        Assert.IsInstanceOfType(parsed, typeof(MultiPoint));
        CollectionAssert.AreEqual(multiPoint.Points.ToArray(), parsed.Points.ToArray());
    }

    [TestMethod]
    public void Render_and_parse_multilinestring_round_trip()
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

        var wkt = GeoFormats.Render(multiLineString);
        var parsed = GeoFormats.Parse<MultiLineString>(wkt);

        Assert.AreEqual("MULTILINESTRING((-46.63 -23.55, -46.64 -23.56), (-46.65 -23.57, -46.66 -23.58))", wkt);
        Assert.IsInstanceOfType(parsed, typeof(MultiLineString));
        Assert.AreEqual(multiLineString.LineStrings.Count, parsed.LineStrings.Count);
        for (var i = 0; i < multiLineString.LineStrings.Count; i++)
        {
            AssertGeometryEquivalent(multiLineString.LineStrings[i], parsed.LineStrings[i]);
        }
    }

    [TestMethod]
    public void Render_and_parse_multipolygon_round_trip()
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

        var wkt = GeoFormats.Render(multiPolygon);
        var parsed = GeoFormats.Parse<MultiPolygon>(wkt);

        Assert.AreEqual("MULTIPOLYGON(((-46.63 -23.55, -46.64 -23.56, -46.65 -23.57, -46.63 -23.55)))", wkt);
        Assert.IsInstanceOfType(parsed, typeof(MultiPolygon));
        Assert.AreEqual(multiPolygon.Polygons.Count, parsed.Polygons.Count);
        for (var i = 0; i < multiPolygon.Polygons.Count; i++)
        {
            AssertGeometryEquivalent(multiPolygon.Polygons[i], parsed.Polygons[i]);
        }
    }

    [TestMethod]
    public void Render_and_parse_geometrycollection_round_trip()
    {
        var collection = new GeometryCollection(new GeoGeometry[]
        {
            new GeoPoint(-23.55, -46.63),
            new LineString(new[]
            {
                new GeoPoint(-23.56, -46.64),
                new GeoPoint(-23.57, -46.65)
            }),
            new Polygon(new[]
            {
                new GeoPoint(-23.58, -46.66),
                new GeoPoint(-23.59, -46.67),
                new GeoPoint(-23.60, -46.68),
                new GeoPoint(-23.58, -46.66)
            })
        });

        var wkt = GeoFormats.Render(collection);
        var parsed = GeoFormats.Parse<GeometryCollection>(wkt);

        Assert.AreEqual(
            "GEOMETRYCOLLECTION(POINT(-46.63 -23.55), LINESTRING(-46.64 -23.56, -46.65 -23.57), POLYGON((-46.66 -23.58, -46.67 -23.59, -46.68 -23.6, -46.66 -23.58)))",
            wkt);
        Assert.IsInstanceOfType(parsed, typeof(GeometryCollection));
        Assert.AreEqual(collection.Geometries.Count, parsed.Geometries.Count);
        AssertGeometryEquivalent(collection.Geometries[0], parsed.Geometries[0]);
        AssertGeometryEquivalent(collection.Geometries[1], parsed.Geometries[1]);
        AssertGeometryEquivalent(collection.Geometries[2], parsed.Geometries[2]);
    }

    [TestMethod]
    public void Parse_rejects_invalid_wkt()
    {
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<GeoPoint>(""));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<GeoPoint>("POINT()"));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<GeoPoint>("POINT(1)"));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<GeoPoint>("POINT(1 2 3)"));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<LineString>("LINESTRING(1 2)"));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<Polygon>("POLYGON((1 2, 3 4, 5 6, 7 8))"));
        Assert.ThrowsException<FormatException>(() => GeoFormats.Parse<GeometryCollection>("GEOMETRYCOLLECTION(UNKNOWN(1 2))"));
    }

    [TestMethod]
    public void Parse_rejects_unclosed_polygon()
    {
        Assert.ThrowsException<FormatException>(() =>
            GeoFormats.Parse<Polygon>("POLYGON((1 2, 3 4, 5 6, 7 8))"));
    }

    private static void AssertGeometryEquivalent(GeoGeometry expected, GeoGeometry actual)
    {
        Assert.AreEqual(expected.GetType(), actual.GetType());

        switch (expected)
        {
            case GeoPoint expectedPoint when actual is GeoPoint actualPoint:
                Assert.AreEqual(expectedPoint, actualPoint);
                break;
            case LineString expectedLine when actual is LineString actualLine:
                CollectionAssert.AreEqual(expectedLine.Points.ToArray(), actualLine.Points.ToArray());
                break;
            case Polygon expectedPolygon when actual is Polygon actualPolygon:
                CollectionAssert.AreEqual(expectedPolygon.OuterRing.ToArray(), actualPolygon.OuterRing.ToArray());
                break;
            case MultiPoint expectedMultiPoint when actual is MultiPoint actualMultiPoint:
                CollectionAssert.AreEqual(expectedMultiPoint.Points.ToArray(), actualMultiPoint.Points.ToArray());
                break;
            case MultiLineString expectedMultiLine when actual is MultiLineString actualMultiLine:
                Assert.AreEqual(expectedMultiLine.LineStrings.Count, actualMultiLine.LineStrings.Count);
                for (var i = 0; i < expectedMultiLine.LineStrings.Count; i++)
                {
                    CollectionAssert.AreEqual(
                        expectedMultiLine.LineStrings[i].Points.ToArray(),
                        actualMultiLine.LineStrings[i].Points.ToArray());
                }
                break;
            case MultiPolygon expectedMultiPolygon when actual is MultiPolygon actualMultiPolygon:
                Assert.AreEqual(expectedMultiPolygon.Polygons.Count, actualMultiPolygon.Polygons.Count);
                for (var i = 0; i < expectedMultiPolygon.Polygons.Count; i++)
                {
                    CollectionAssert.AreEqual(
                        expectedMultiPolygon.Polygons[i].OuterRing.ToArray(),
                        actualMultiPolygon.Polygons[i].OuterRing.ToArray());
                }
                break;
            case GeometryCollection expectedCollection when actual is GeometryCollection actualCollection:
                Assert.AreEqual(expectedCollection.Geometries.Count, actualCollection.Geometries.Count);
                for (var i = 0; i < expectedCollection.Geometries.Count; i++)
                {
                    AssertGeometryEquivalent(expectedCollection.Geometries[i], actualCollection.Geometries[i]);
                }
                break;
            default:
                Assert.Fail($"Unsupported geometry type: {expected.GetType().Name}");
                break;
        }
    }
}
