namespace AdaptadorGEO.Geometry;

public sealed record GeoPoint : GeoGeometry
{
    public GeoPoint(double latitude, double longitude)
    {
        CoordinateRange.ValidateLatitude(latitude);
        CoordinateRange.ValidateLongitude(longitude);

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }
}
