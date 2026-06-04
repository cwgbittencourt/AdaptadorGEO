namespace AdaptadorGEO.Geometry;

public sealed record MultiLineString : GeoGeometry
{
    public MultiLineString(IReadOnlyList<LineString> lineStrings)
    {
        if (lineStrings.Count == 0)
        {
            throw new ArgumentException("A MultiLineString must contain at least one line string.", nameof(lineStrings));
        }

        LineStrings = lineStrings.ToArray();
    }

    public IReadOnlyList<LineString> LineStrings { get; }
}
