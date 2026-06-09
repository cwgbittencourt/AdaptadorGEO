using AdaptadorGEO.Formats.GeoJson;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GeoJsonApi = AdaptadorGEO.Formats.GeoJson.GeoJson;

namespace AdaptadorGEO.Tests.Formats.GeoJson;

[TestClass]
public class GeoJsonValidationTests
{
    [TestMethod]
    public void ReadGeometry_ShouldRejectNullJson()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(null!));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectEmptyJson()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(string.Empty));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectInvalidJson()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry("{ invalid json }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectMissingType()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""coordinates"": [1, 2] }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectMissingCoordinates()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""Point"" }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectUnsupportedType()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""Unknown"" }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectPointWithInsufficientCoordinates()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""Point"", ""coordinates"": [-46.63] }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectLineStringWithOnePoint()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""LineString"", ""coordinates"": [[-46.63, -23.55]] }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectOpenPolygon()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"
{
    ""type"": ""Polygon"",
    ""coordinates"": [
        [
            [-46.63, -23.55],
            [-46.64, -23.56],
            [-46.65, -23.57],
            [-46.66, -23.58]
        ]
    ]
}"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectFeatureWithoutGeometry()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""Feature"", ""properties"": { ""nome"": ""x"" } }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectFeatureCollectionWithoutFeatures()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"{ ""type"": ""FeatureCollection"" }"));
    }

    [TestMethod]
    public void ReadGeometry_ShouldRejectFeatureCollectionWithPolygonHoles()
    {
        Assert.ThrowsException<GeoJsonException>(() => GeoJsonApi.ReadGeometry(@"
{
    ""type"": ""FeatureCollection"",
    ""features"": [
        {
            ""type"": ""Feature"",
            ""geometry"": {
                ""type"": ""Polygon"",
                ""coordinates"": [
                    [
                        [-46.63, -23.55],
                        [-46.64, -23.56],
                        [-46.65, -23.57],
                        [-46.63, -23.55]
                    ],
                    [
                        [-46.61, -23.51],
                        [-46.62, -23.52],
                        [-46.63, -23.53],
                        [-46.61, -23.51]
                    ]
                ]
            }
        }
    ]
}"));
    }
}
