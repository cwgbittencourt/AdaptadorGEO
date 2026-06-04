namespace AdaptadorGEO.Geometry;

public sealed record MultiPoint : GeoGeometry
{
    public MultiPoint(IReadOnlyList<GeoPoint> points)
    {
        if (points.Count == 0)
        {
            throw new ArgumentException("A MultiPoint must contain at least one point.", nameof(points));
        }

        Points = points.ToArray();
    }

    public IReadOnlyList<GeoPoint> Points { get; }
}
