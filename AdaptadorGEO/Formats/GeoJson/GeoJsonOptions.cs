namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonOptions
{
    public bool Indented { get; init; }

    public int? Srid { get; init; } = 4326;

    public bool IncludeCrs { get; init; }
}
