using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class GeoPointTests
{
    [TestMethod]
    public void Constructor_rejects_latitude_outside_range()
    {
        var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() => new GeoPoint(91, 10));
        Assert.AreEqual("latitude", ex.ParamName);
    }

    [TestMethod]
    public void Constructor_rejects_longitude_outside_range()
    {
        var ex = Assert.ThrowsException<ArgumentOutOfRangeException>(() => new GeoPoint(10, 181));
        Assert.AreEqual("longitude", ex.ParamName);
    }

    [TestMethod]
    public void Point_preserves_latitude_and_longitude_order()
    {
        var point = new GeoPoint(-23.55052, -46.63331);

        Assert.AreEqual(-23.55052, point.Latitude);
        Assert.AreEqual(-46.63331, point.Longitude);
    }
}
