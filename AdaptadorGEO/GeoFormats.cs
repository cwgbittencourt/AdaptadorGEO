using AdaptadorGEO.Geometry;

namespace AdaptadorGEO;

public static class GeoFormats
{
    public static string Render(GeoGeometry geometry) => GeoWkt.Render(geometry);

    public static GeoGeometry Parse(string wkt) => GeoWktParser.Parse(wkt);

    public static T Parse<T>(string wkt) where T : GeoGeometry => (T)Parse(wkt);
}
