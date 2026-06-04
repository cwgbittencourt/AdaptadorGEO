namespace AdaptadorGEO.Geometry;

public sealed record GeometryCollection : GeoGeometry
{
    public GeometryCollection(IReadOnlyList<GeoGeometry> geometries)
    {
        if (geometries.Count == 0)
        {
            throw new ArgumentException("A GeometryCollection must contain at least one geometry.", nameof(geometries));
        }

        Geometries = geometries.ToArray();
    }

    public IReadOnlyList<GeoGeometry> Geometries { get; }
}
