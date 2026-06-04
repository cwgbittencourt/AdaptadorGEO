using System.Globalization;

namespace AdaptadorGEO.Geometry;

internal static class GeoWkt
{
    public static string Render(GeoGeometry geometry) =>
        geometry switch
        {
            GeoPoint point => RenderPoint(point),
            LineString lineString => RenderLineString(lineString),
            Polygon polygon => RenderPolygon(polygon),
            MultiPoint multiPoint => RenderMultiPoint(multiPoint),
            MultiLineString multiLineString => RenderMultiLineString(multiLineString),
            MultiPolygon multiPolygon => RenderMultiPolygon(multiPolygon),
            GeometryCollection collection => RenderGeometryCollection(collection),
            _ => throw new NotSupportedException($"Unsupported geometry type: {geometry.GetType().Name}")
        };

    private static string RenderPoint(GeoPoint point) =>
        FormattableString.Invariant($"POINT({point.Longitude} {point.Latitude})");

    private static string RenderLineString(LineString lineString)
    {
        var coordinates = string.Join(
            ", ",
            lineString.Points.Select(point => FormattableString.Invariant($"{point.Longitude} {point.Latitude}")));

        return $"LINESTRING({coordinates})";
    }

    private static string RenderPolygon(Polygon polygon)
    {
        var coordinates = string.Join(
            ", ",
            polygon.OuterRing.Select(point => FormattableString.Invariant($"{point.Longitude} {point.Latitude}")));

        return $"POLYGON(({coordinates}))";
    }

    private static string RenderMultiPoint(MultiPoint multiPoint)
    {
        var points = string.Join(
            ", ",
            multiPoint.Points.Select(point => $"({FormattableString.Invariant($"{point.Longitude} {point.Latitude}")})"));

        return $"MULTIPOINT({points})";
    }

    private static string RenderMultiLineString(MultiLineString multiLineString)
    {
        var lines = string.Join(
            ", ",
            multiLineString.LineStrings.Select(lineString =>
            {
                var coordinates = string.Join(
                    ", ",
                    lineString.Points.Select(point => FormattableString.Invariant($"{point.Longitude} {point.Latitude}")));

                return $"({coordinates})";
            }));

        return $"MULTILINESTRING({lines})";
    }

    private static string RenderMultiPolygon(MultiPolygon multiPolygon)
    {
        var polygons = string.Join(
            ", ",
            multiPolygon.Polygons.Select(polygon =>
            {
                var coordinates = string.Join(
                    ", ",
                    polygon.OuterRing.Select(point => FormattableString.Invariant($"{point.Longitude} {point.Latitude}")));

                return $"(({coordinates}))";
            }));

        return $"MULTIPOLYGON({polygons})";
    }

    private static string RenderGeometryCollection(GeometryCollection collection)
    {
        var geometries = string.Join(", ", collection.Geometries.Select(Render));
        return $"GEOMETRYCOLLECTION({geometries})";
    }
}
