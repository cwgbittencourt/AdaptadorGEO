using AdaptadorGEO.Formats.GeoJson;
using AdaptadorGEO.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Formats.GeoJson;

[TestClass]
public class GeoJsonFeatureTests
{
    [TestMethod]
    public void Feature_ShouldExposeGeometryAndProperties()
    {
        var feature = new GeoJsonFeature(
            new GeoPoint(-23.55052, -46.63331),
            new Dictionary<string, object?>
            {
                ["nome"] = "Garagem Centro",
                ["ativo"] = true
            });

        Assert.IsInstanceOfType(feature.Geometry, typeof(GeoPoint));
        Assert.AreEqual("Garagem Centro", feature.Properties["nome"]);
        Assert.AreEqual(true, feature.Properties["ativo"]);
    }

    [TestMethod]
    public void FeatureCollection_ShouldExposeFeatures()
    {
        var feature = new GeoJsonFeature(new GeoPoint(-23.55052, -46.63331));
        var collection = new GeoJsonFeatureCollection(new[] { feature });

        Assert.AreEqual(1, collection.Features.Count);
        Assert.AreSame(feature, collection.Features[0]);
    }
}
