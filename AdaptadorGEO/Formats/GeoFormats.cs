using AdaptadorGEO.Geometry;
using GeoJsonApi = AdaptadorGEO.Formats.GeoJson.GeoJson;

namespace AdaptadorGEO.Formats;

public static class GeoFormats
{
    public static string ToGeoJson(GeoGeometry geometry) => GeoJsonApi.Write(geometry);

    public static GeoGeometry FromGeoJson(string geoJson) => GeoJsonApi.ReadGeometry(geoJson);
}
