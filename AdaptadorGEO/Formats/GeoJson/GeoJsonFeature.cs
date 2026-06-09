using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonFeature
{
    public GeoJsonFeature(
        GeoGeometry geometry,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        Properties = properties ?? new Dictionary<string, object?>();
    }

    public GeoGeometry Geometry { get; }

    public IReadOnlyDictionary<string, object?> Properties { get; }
}
