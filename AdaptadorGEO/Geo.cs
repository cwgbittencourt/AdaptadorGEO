using AdaptadorGEO.Geometry;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO;

public static class Geo
{
    public static GeoPoint Point(double latitude, double longitude) => new(latitude, longitude);

    public static LineString LineString(params GeoPoint[] points) => new(points);

    public static Polygon Polygon(params GeoPoint[] outerRing) => new(outerRing);

    public static MultiPoint MultiPoint(params GeoPoint[] points) => new(points);

    public static MultiLineString MultiLineString(params LineString[] lineStrings) => new(lineStrings);

    public static MultiPolygon MultiPolygon(params Polygon[] polygons) => new(polygons);

    public static GeometryCollection GeometryCollection(params GeoGeometry[] geometries) => new(geometries);

    public static GeoColumn Column(string name) => new(name);

    public static GeoLiteral Literal(GeoGeometry geometry) => new(geometry);
}

public static class GeoExpressionExtensions
{
    public static GeoBufferExpression Buffer(this GeoExpression source, double distanceMeters) =>
        new(source, distanceMeters);

    public static GeoIntersectsExpression Intersects(this GeoExpression left, GeoGeometry right) =>
        new(left, Geo.Literal(right));

    public static GeoContainsExpression Contains(this GeoExpression left, GeoGeometry right) =>
        new(left, Geo.Literal(right));

    public static GeoWithinExpression Within(this GeoExpression left, GeoGeometry right) =>
        new(left, Geo.Literal(right));

    public static GeoDistanceExpression Distance(this GeoExpression left, GeoGeometry right) =>
        new(left, Geo.Literal(right));
}
