using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Geometry;

[TestClass]
public class MultiLineStringTests
{
    [TestMethod]
    public void Constructor_rejects_empty_line_collection()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => new MultiLineString(Array.Empty<LineString>()));

        StringAssert.Contains(ex.Message, "at least one line string");
    }

    [TestMethod]
    public void Constructor_preserves_line_strings()
    {
        var line1 = new LineString(new[]
        {
            new GeoPoint(-23.55, -46.63),
            new GeoPoint(-23.56, -46.64)
        });
        var line2 = new LineString(new[]
        {
            new GeoPoint(-23.57, -46.65),
            new GeoPoint(-23.58, -46.66)
        });

        var multiLineString = new MultiLineString(new[] { line1, line2 });

        Assert.AreEqual(2, multiLineString.LineStrings.Count);
        Assert.AreEqual(line1, multiLineString.LineStrings[0]);
        Assert.AreEqual(line2, multiLineString.LineStrings[1]);
    }
}
