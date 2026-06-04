using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class PolygonTests
{
    [TestMethod]
    public void Constructor_rejects_outer_ring_with_less_than_four_points()
    {
        var points = new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65)
        };

        var ex = Assert.ThrowsException<ArgumentException>(() => new Polygon(points));

        StringAssert.Contains(ex.Message, "at least four points");
    }

    [TestMethod]
    public void Constructor_rejects_unclosed_outer_ring()
    {
        var points = new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.58, -46.66)
        };

        var ex = Assert.ThrowsException<ArgumentException>(() => new Polygon(points));

        StringAssert.Contains(ex.Message, "must be closed");
    }

    [TestMethod]
    public void Constructor_preserves_closed_outer_ring()
    {
        var first = new GeoPoint(-23.55, -46.63);
        var points = new[]
        {
            first,
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            first
        };

        var polygon = new Polygon(points);

        Assert.AreEqual(4, polygon.OuterRing.Count);
        Assert.AreEqual(points[0], polygon.OuterRing[0]);
        Assert.AreEqual(points[3], polygon.OuterRing[3]);
    }
}
