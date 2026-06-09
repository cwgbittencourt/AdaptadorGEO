namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonFeatureCollection
{
    public GeoJsonFeatureCollection(IReadOnlyList<GeoJsonFeature> features)
    {
        Features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public IReadOnlyList<GeoJsonFeature> Features { get; }
}
