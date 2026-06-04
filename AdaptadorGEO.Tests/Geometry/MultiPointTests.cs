using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class MultiPointTests
{
    [TestMethod]
    public void Constructor_rejects_empty_point_collection()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => new MultiPoint(Array.Empty<GeoPoint>()));

        StringAssert.Contains(ex.Message, "at least one point");
    }

    [TestMethod]
    public void Constructor_preserves_points()
    {
        var points = new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        };

        var multiPoint = new MultiPoint(points);

        Assert.AreEqual(2, multiPoint.Points.Count);
        Assert.AreEqual(points[0], multiPoint.Points[0]);
        Assert.AreEqual(points[1], multiPoint.Points[1]);
    }
}
