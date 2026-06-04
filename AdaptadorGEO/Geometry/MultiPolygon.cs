namespace AdaptadorGEO.Geometry;

public sealed record MultiPolygon : GeoGeometry
{
    public MultiPolygon(IReadOnlyList<Polygon> polygons)
    {
        if (polygons.Count == 0)
        {
            throw new ArgumentException("A MultiPolygon must contain at least one polygon.", nameof(polygons));
        }

        Polygons = polygons.ToArray();
    }

    public IReadOnlyList<Polygon> Polygons { get; }
}
