namespace AdaptadorGEO.Geometry;

public sealed record Polygon : GeoGeometry
{
    public Polygon(IReadOnlyList<GeoPoint> outerRing)
    {
        if (outerRing.Count < 4)
        {
            throw new ArgumentException("A Polygon outer ring must contain at least four points.", nameof(outerRing));
        }

        if (!outerRing[0].Equals(outerRing[^1]))
        {
            throw new ArgumentException("A Polygon outer ring must be closed.", nameof(outerRing));
        }

        OuterRing = outerRing.ToArray();
    }

    public IReadOnlyList<GeoPoint> OuterRing { get; }
}
