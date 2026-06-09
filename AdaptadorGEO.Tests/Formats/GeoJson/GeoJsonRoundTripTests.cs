using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Formats.GeoJson;

[TestClass]
public class GeoJsonRoundTripTests
{
    [TestMethod]
    public void RoundTrip_Point_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new GeoPoint(-23.55052, -46.63331));
    }

    [TestMethod]
    public void RoundTrip_LineString_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new LineString(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        }));
    }

    [TestMethod]
    public void RoundTrip_Polygon_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new Polygon(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.55, -46.63)
        }));
    }

    [TestMethod]
    public void RoundTrip_MultiPoint_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new MultiPoint(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        }));
    }

    [TestMethod]
    public void RoundTrip_MultiLineString_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new MultiLineString(new[]
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
        }));
    }

    [TestMethod]
    public void RoundTrip_MultiPolygon_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new MultiPolygon(new[]
        {
            new Polygon(new[]
            {
                new GeoPoint(-23.55, -46.63),
                new GeoPoint(-23.56, -46.64),
                new GeoPoint(-23.57, -46.65),
                new GeoPoint(-23.55, -46.63)
            })
        }));
    }

    [TestMethod]
    public void RoundTrip_GeometryCollection_ShouldPreserveCoordinates()
    {
        RoundTripShouldPreserveGeometry(new GeometryCollection(new GeoGeometry[]
        {
            new GeoPoint(-23.55, -46.63),
            new LineString(new[]
            {
                new GeoPoint(-23.56, -46.64),
                new GeoPoint(-23.57, -46.65)
            })
        }));
    }

    private static void RoundTripShouldPreserveGeometry(GeoGeometry expected)
    {
        var json = AdaptadorGEO.Formats.GeoFormats.ToGeoJson(expected);
        var parsed = AdaptadorGEO.Formats.GeoFormats.FromGeoJson(json);

        AssertGeometryEquivalent(expected, parsed);
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
                Assert.AreEqual(expectedLine.Points.Count, actualLine.Points.Count);
                for (var i = 0; i < expectedLine.Points.Count; i++)
                {
                    Assert.AreEqual(expectedLine.Points[i], actualLine.Points[i]);
                }
                break;
            case Polygon expectedPolygon when actual is Polygon actualPolygon:
                Assert.AreEqual(expectedPolygon.OuterRing.Count, actualPolygon.OuterRing.Count);
                for (var i = 0; i < expectedPolygon.OuterRing.Count; i++)
                {
                    Assert.AreEqual(expectedPolygon.OuterRing[i], actualPolygon.OuterRing[i]);
                }
                break;
            case MultiPoint expectedMultiPoint when actual is MultiPoint actualMultiPoint:
                Assert.AreEqual(expectedMultiPoint.Points.Count, actualMultiPoint.Points.Count);
                for (var i = 0; i < expectedMultiPoint.Points.Count; i++)
                {
                    Assert.AreEqual(expectedMultiPoint.Points[i], actualMultiPoint.Points[i]);
                }
                break;
            case MultiLineString expectedMultiLine when actual is MultiLineString actualMultiLine:
                Assert.AreEqual(expectedMultiLine.LineStrings.Count, actualMultiLine.LineStrings.Count);
                for (var i = 0; i < expectedMultiLine.LineStrings.Count; i++)
                {
                    Assert.AreEqual(expectedMultiLine.LineStrings[i].Points.Count, actualMultiLine.LineStrings[i].Points.Count);
                    for (var j = 0; j < expectedMultiLine.LineStrings[i].Points.Count; j++)
                    {
                        Assert.AreEqual(expectedMultiLine.LineStrings[i].Points[j], actualMultiLine.LineStrings[i].Points[j]);
                    }
                }
                break;
            case MultiPolygon expectedMultiPolygon when actual is MultiPolygon actualMultiPolygon:
                Assert.AreEqual(expectedMultiPolygon.Polygons.Count, actualMultiPolygon.Polygons.Count);
                for (var i = 0; i < expectedMultiPolygon.Polygons.Count; i++)
                {
                    Assert.AreEqual(expectedMultiPolygon.Polygons[i].OuterRing.Count, actualMultiPolygon.Polygons[i].OuterRing.Count);
                    for (var j = 0; j < expectedMultiPolygon.Polygons[i].OuterRing.Count; j++)
                    {
                        Assert.AreEqual(expectedMultiPolygon.Polygons[i].OuterRing[j], actualMultiPolygon.Polygons[i].OuterRing[j]);
                    }
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
