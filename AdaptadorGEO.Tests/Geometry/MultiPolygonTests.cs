using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class MultiPolygonTests
{
    [TestMethod]
    public void Constructor_rejects_empty_polygon_collection()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => new MultiPolygon(Array.Empty<Polygon>()));

        StringAssert.Contains(ex.Message, "at least one polygon");
    }

    [TestMethod]
    public void Constructor_preserves_polygons()
    {
        var ring = new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64),
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.55, -46.63)
        };
        var polygon = new Polygon(ring);

        var multiPolygon = new MultiPolygon(new[] { polygon });

        Assert.AreEqual(1, multiPolygon.Polygons.Count);
        Assert.AreEqual(polygon, multiPolygon.Polygons[0]);
    }
}
