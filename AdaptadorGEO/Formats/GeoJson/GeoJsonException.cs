namespace AdaptadorGEO.Formats.GeoJson;

public sealed class GeoJsonException : Exception
{
    public GeoJsonException(string message)
        : base(message)
    {
    }

    public GeoJsonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
