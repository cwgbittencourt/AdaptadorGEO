using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class LineStringTests
{
    [TestMethod]
    public void Constructor_rejects_less_than_two_points()
    {
        var points = new[] { new GeoPoint(-23.55, -46.63) };

        var ex = Assert.ThrowsException<ArgumentException>(() => new LineString(points));

        StringAssert.Contains(ex.Message, "at least two points");
    }

    [TestMethod]
    public void Constructor_preserves_points_in_order()
    {
        var points = new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        };

        var line = new LineString(points);

        Assert.AreEqual(2, line.Points.Count);
        Assert.AreEqual(points[0], line.Points[0]);
        Assert.AreEqual(points[1], line.Points[1]);
    }
}
