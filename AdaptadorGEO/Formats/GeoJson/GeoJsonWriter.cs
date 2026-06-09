using System.Text;
using System.Text.Json;
using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

internal static class GeoJsonWriter
{
    public static string Write(GeoGeometry geometry, GeoJsonOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(geometry);

        options ??= new GeoJsonOptions();

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options.Indented }))
        {
            WriteGeometry(writer, geometry, options, includeCrs: true);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteGeometry(Utf8JsonWriter writer, GeoGeometry geometry, GeoJsonOptions options, bool includeCrs)
    {
        writer.WriteStartObject();

        switch (geometry)
        {
            case GeoPoint point:
                writer.WriteString("type", "Point");
                writer.WritePropertyName("coordinates");
                WritePosition(writer, point);
                break;
            case LineString lineString:
                writer.WriteString("type", "LineString");
                writer.WritePropertyName("coordinates");
                WritePositionArray(writer, lineString.Points);
                break;
            case Polygon polygon:
                writer.WriteString("type", "Polygon");
                writer.WritePropertyName("coordinates");
                writer.WriteStartArray();
                writer.WriteStartArray();
                WritePositionArrayContents(writer, polygon.OuterRing);
                writer.WriteEndArray();
                writer.WriteEndArray();
                break;
            case MultiPoint multiPoint:
                writer.WriteString("type", "MultiPoint");
                writer.WritePropertyName("coordinates");
                WritePositionArray(writer, multiPoint.Points);
                break;
            case MultiLineString multiLineString:
                writer.WriteString("type", "MultiLineString");
                writer.WritePropertyName("coordinates");
                writer.WriteStartArray();
                foreach (var lineString in multiLineString.LineStrings)
                {
                    WritePositionArray(writer, lineString.Points);
                }
                writer.WriteEndArray();
                break;
            case MultiPolygon multiPolygon:
                writer.WriteString("type", "MultiPolygon");
                writer.WritePropertyName("coordinates");
                writer.WriteStartArray();
                foreach (var polygon in multiPolygon.Polygons)
                {
                    writer.WriteStartArray();
                    writer.WriteStartArray();
                    WritePositionArrayContents(writer, polygon.OuterRing);
                    writer.WriteEndArray();
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                break;
            case GeometryCollection geometryCollection:
                writer.WriteString("type", "GeometryCollection");
                writer.WritePropertyName("geometries");
                writer.WriteStartArray();
                foreach (var child in geometryCollection.Geometries)
                {
                    WriteGeometry(writer, child, options, includeCrs: false);
                }
                writer.WriteEndArray();
                break;
            default:
                throw new GeoJsonException($"Unsupported geometry type: {geometry.GetType().Name}");
        }

        if (includeCrs && options.IncludeCrs && options.Srid.HasValue)
        {
            writer.WritePropertyName("crs");
            writer.WriteStartObject();
            writer.WriteString("type", "name");
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WriteString("name", $"EPSG:{options.Srid.Value}");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private static void WritePositionArray(Utf8JsonWriter writer, IReadOnlyList<GeoPoint> points)
    {
        writer.WriteStartArray();
        WritePositionArrayContents(writer, points);
        writer.WriteEndArray();
    }

    private static void WritePositionArrayContents(Utf8JsonWriter writer, IReadOnlyList<GeoPoint> points)
    {
        foreach (var point in points)
        {
            WritePosition(writer, point);
        }
    }

    private static void WritePosition(Utf8JsonWriter writer, GeoPoint point)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(point.Longitude);
        writer.WriteNumberValue(point.Latitude);
        writer.WriteEndArray();
    }
}
