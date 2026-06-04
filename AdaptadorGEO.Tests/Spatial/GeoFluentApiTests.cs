using AdaptadorGEO;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Spatial;

[TestClass]
public class GeoFluentApiTests
{
    [TestMethod]
    public void Point_factory_creates_a_valid_point()
    {
        var point = Geo.Point(-23.55052, -46.63331);

        Assert.AreEqual(-23.55052, point.Latitude);
        Assert.AreEqual(-46.63331, point.Longitude);
    }

    [TestMethod]
    public void LineString_factory_creates_a_valid_line_string()
    {
        var line = Geo.LineString(
            Geo.Point(-23.55, -46.63),
            Geo.Point(-23.56, -46.64));

        Assert.AreEqual(2, line.Points.Count);
    }

    [TestMethod]
    public void Polygon_factory_creates_a_valid_polygon()
    {
        var polygon = Geo.Polygon(
            Geo.Point(-23.55, -46.63),
            Geo.Point(-23.56, -46.64),
            Geo.Point(-23.57, -46.65),
            Geo.Point(-23.55, -46.63));

        Assert.AreEqual(4, polygon.OuterRing.Count);
    }

    [TestMethod]
    public void MultiLineString_factory_creates_a_valid_multi_line_string()
    {
        var multiLineString = Geo.MultiLineString(
            Geo.LineString(Geo.Point(-23.55, -46.63), Geo.Point(-23.56, -46.64)),
            Geo.LineString(Geo.Point(-23.57, -46.65), Geo.Point(-23.58, -46.66)));

        Assert.AreEqual(2, multiLineString.LineStrings.Count);
    }

    [TestMethod]
    public void MultiPolygon_factory_creates_a_valid_multi_polygon()
    {
        var multiPolygon = Geo.MultiPolygon(
            Geo.Polygon(
                Geo.Point(-23.55, -46.63),
                Geo.Point(-23.56, -46.64),
                Geo.Point(-23.57, -46.65),
                Geo.Point(-23.55, -46.63)));

        Assert.AreEqual(1, multiPolygon.Polygons.Count);
    }

    [TestMethod]
    public void Column_intersects_point_creates_intersects_expression()
    {
        var expr = Geo.Column("Area").Intersects(Geo.Point(-23.55, -46.63));

        Assert.IsInstanceOfType(expr, typeof(GeoIntersectsExpression));
    }

    [TestMethod]
    public void Column_buffer_creates_buffer_expression()
    {
        var expr = Geo.Column("Area").Buffer(250);

        Assert.IsInstanceOfType(expr, typeof(GeoBufferExpression));
    }

    [TestMethod]
    public void Column_contains_point_creates_contains_expression()
    {
        var expr = Geo.Column("Area").Contains(Geo.Point(-23.55, -46.63));

        Assert.IsInstanceOfType(expr, typeof(GeoContainsExpression));
    }

    [TestMethod]
    public void Column_within_point_creates_within_expression()
    {
        var expr = Geo.Column("Area").Within(Geo.Point(-23.55, -46.63));

        Assert.IsInstanceOfType(expr, typeof(GeoWithinExpression));
    }

    [TestMethod]
    public void Column_distance_point_creates_distance_expression()
    {
        var expr = Geo.Column("Area").Distance(Geo.Point(-23.55, -46.63));

        Assert.IsInstanceOfType(expr, typeof(GeoDistanceExpression));
    }
}
