namespace AdaptadorGEO.Geometry;

public static class CoordinateRange
{
    public static void ValidateLatitude(double latitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        }
    }

    public static void ValidateLongitude(double longitude)
    {
        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        }
    }
}
