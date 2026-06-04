namespace AdaptadorGEO.Geometry;

public sealed record LineString : GeoGeometry
{
    public LineString(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count < 2)
        {
            throw new ArgumentException("A LineString must contain at least two points.", nameof(points));
        }

        Points = points.ToArray();
    }

    public IReadOnlyList<GeoPoint> Points { get; }
}
