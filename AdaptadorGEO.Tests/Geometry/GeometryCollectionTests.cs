using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class GeometryCollectionTests
{
    [TestMethod]
    public void Constructor_rejects_empty_collection()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => new GeometryCollection(Array.Empty<GeoGeometry>()));

        StringAssert.Contains(ex.Message, "at least one geometry");
    }

    [TestMethod]
    public void Constructor_preserves_mixed_geometries()
    {
        var geometries = new GeoGeometry[]
        {
            new GeoPoint(-23.55, -46.63),
            new MultiPoint(new[] { new GeoPoint(-23.56, -46.64) })
        };

        var collection = new GeometryCollection(geometries);

        Assert.AreEqual(2, collection.Geometries.Count);
        Assert.AreEqual(geometries[0], collection.Geometries[0]);
        Assert.AreEqual(geometries[1], collection.Geometries[1]);
    }
}
