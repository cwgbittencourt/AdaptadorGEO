using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Spatial;

[TestClass]
public class GeoExpressionTests
{
    [TestMethod]
    public void Buffer_expression_exposes_input_geometry_and_distance()
    {
        var expr = new GeoBufferExpression(new GeoColumn("Location"), 250);

        Assert.AreEqual(250, expr.DistanceMeters);
        Assert.IsInstanceOfType(expr.Source, typeof(GeoColumn));
    }

    [TestMethod]
    public void Intersects_expression_keeps_both_operands()
    {
        var left = new GeoColumn("Area");
        var right = new GeoLiteral(new GeoPoint(-23.55, -46.63));
        var expr = new GeoIntersectsExpression(left, right);

        Assert.AreSame(left, expr.Left);
        Assert.AreSame(right, expr.Right);
    }
}
