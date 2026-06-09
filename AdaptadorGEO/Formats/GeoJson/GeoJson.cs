using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

public static class GeoJson
{
    public static string Write(GeoGeometry geometry, GeoJsonOptions? options = null) =>
        GeoJsonWriter.Write(geometry, options);

    public static GeoGeometry ReadGeometry(string geoJson) => GeoJsonReader.ReadGeometry(geoJson);

    public static GeoJsonFeature ReadFeature(string geoJson) => GeoJsonReader.ReadFeature(geoJson);

    public static GeoJsonFeatureCollection ReadFeatureCollection(string geoJson) => GeoJsonReader.ReadFeatureCollection(geoJson);
}
