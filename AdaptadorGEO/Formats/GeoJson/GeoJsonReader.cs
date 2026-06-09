using System.Text.Json;
using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Formats.GeoJson;

internal static class GeoJsonReader
{
    public static GeoGeometry ReadGeometry(string geoJson)
    {
        using var document = ParseDocument(geoJson);
        return ReadGeometry(document.RootElement);
    }

    public static GeoJsonFeature ReadFeature(string geoJson)
    {
        using var document = ParseDocument(geoJson);
        return ReadFeature(document.RootElement);
    }

    public static GeoJsonFeatureCollection ReadFeatureCollection(string geoJson)
    {
        using var document = ParseDocument(geoJson);
        return ReadFeatureCollection(document.RootElement);
    }

    private static GeoGeometry ReadGeometry(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error("GeoJSON root must be an object.");
        }

        var type = ReadType(element);

        return type switch
        {
            "Point" => ReadPointGeometry(element),
            "LineString" => ReadLineStringGeometry(element),
            "Polygon" => ReadPolygonGeometry(element),
            "MultiPoint" => ReadMultiPointGeometry(element),
            "MultiLineString" => ReadMultiLineStringGeometry(element),
            "MultiPolygon" => ReadMultiPolygonGeometry(element),
            "GeometryCollection" => ReadGeometryCollectionGeometry(element),
            "Feature" => ReadFeature(element).Geometry,
            "FeatureCollection" => ReadFeatureCollectionAsGeometryCollection(element),
            _ => throw Error($"Unsupported GeoJSON type '{type}'.")
        };
    }

    private static GeoJsonFeature ReadFeature(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error("Feature must be a JSON object.");
        }

        if (!string.Equals(ReadType(element), "Feature", StringComparison.Ordinal))
        {
            throw Error("JSON object is not a Feature.");
        }

        if (!TryGetProperty(element, "geometry", out var geometryElement) || geometryElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw Error("Feature must contain a geometry.");
        }

        var geometry = ReadGeometryObject(geometryElement);
        var properties = TryGetProperty(element, "properties", out var propertiesElement) && propertiesElement.ValueKind != JsonValueKind.Null
            ? ReadProperties(propertiesElement)
            : new Dictionary<string, object?>();

        return new GeoJsonFeature(geometry, properties);
    }

    private static GeoJsonFeatureCollection ReadFeatureCollection(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error("FeatureCollection must be a JSON object.");
        }

        if (!string.Equals(ReadType(element), "FeatureCollection", StringComparison.Ordinal))
        {
            throw Error("JSON object is not a FeatureCollection.");
        }

        if (!TryGetProperty(element, "features", out var featuresElement) || featuresElement.ValueKind != JsonValueKind.Array)
        {
            throw Error("FeatureCollection must contain features.");
        }

        var features = new List<GeoJsonFeature>();
        foreach (var featureElement in featuresElement.EnumerateArray())
        {
            features.Add(ReadFeature(featureElement));
        }

        return new GeoJsonFeatureCollection(features);
    }

    private static GeoGeometry ReadFeatureCollectionAsGeometryCollection(JsonElement element)
    {
        var featureCollection = ReadFeatureCollection(element);
        if (featureCollection.Features.Count == 0)
        {
            throw Error("FeatureCollection must contain at least one feature to convert to GeometryCollection.");
        }

        return new GeometryCollection(featureCollection.Features.Select(feature => feature.Geometry).ToArray());
    }

    private static GeoGeometry ReadGeometryObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error("geometry must be a JSON object.");
        }

        var type = ReadType(element);
        return type switch
        {
            "Point" => ReadPointGeometry(element),
            "LineString" => ReadLineStringGeometry(element),
            "Polygon" => ReadPolygonGeometry(element),
            "MultiPoint" => ReadMultiPointGeometry(element),
            "MultiLineString" => ReadMultiLineStringGeometry(element),
            "MultiPolygon" => ReadMultiPolygonGeometry(element),
            "GeometryCollection" => ReadGeometryCollectionGeometry(element),
            _ => throw Error($"Unsupported GeoJSON type '{type}'.")
        };
    }

    private static GeoPoint ReadPointGeometry(JsonElement element)
    {
        var position = ReadPosition(RequireProperty(element, "coordinates", "Point requires coordinates."));
        return Create(() => new GeoPoint(position.Latitude, position.Longitude));
    }

    private static LineString ReadLineStringGeometry(JsonElement element)
    {
        var points = ReadPositionList(RequireProperty(element, "coordinates", "LineString requires coordinates."));
        if (points.Count < 2)
        {
            throw Error("A LineString must contain at least two points.");
        }

        return Create(() => new LineString(points));
    }

    private static Polygon ReadPolygonGeometry(JsonElement element)
    {
        var rings = ReadNestedPositionLists(RequireProperty(element, "coordinates", "Polygon requires coordinates."));
        if (rings.Count == 0)
        {
            throw Error("A Polygon must contain at least one ring.");
        }

        if (rings.Count != 1)
        {
            throw Error("A Polygon with holes is not supported.");
        }

        var outerRing = rings[0];
        if (outerRing.Count < 4)
        {
            throw Error("A Polygon outer ring must contain at least four points.");
        }

        return Create(() => new Polygon(outerRing));
    }

    private static MultiPoint ReadMultiPointGeometry(JsonElement element)
    {
        var points = ReadPositionList(RequireProperty(element, "coordinates", "MultiPoint requires coordinates."));
        if (points.Count == 0)
        {
            throw Error("A MultiPoint must contain at least one point.");
        }

        return Create(() => new MultiPoint(points));
    }

    private static MultiLineString ReadMultiLineStringGeometry(JsonElement element)
    {
        var lineSets = ReadNestedPositionLists(RequireProperty(element, "coordinates", "MultiLineString requires coordinates."));
        if (lineSets.Count == 0)
        {
            throw Error("A MultiLineString must contain at least one line string.");
        }

        var lineStrings = lineSets.Select(points =>
        {
            if (points.Count < 2)
            {
                throw Error("A LineString must contain at least two points.");
            }

            return Create(() => new LineString(points));
        }).ToArray();

        return Create(() => new MultiLineString(lineStrings));
    }

    private static MultiPolygon ReadMultiPolygonGeometry(JsonElement element)
    {
        var polygonSets = ReadPolygonLists(RequireProperty(element, "coordinates", "MultiPolygon requires coordinates."));
        if (polygonSets.Count == 0)
        {
            throw Error("A MultiPolygon must contain at least one polygon.");
        }

        var polygons = polygonSets.Select(outerRing =>
        {
            if (outerRing.Count < 4)
            {
                throw Error("A Polygon outer ring must contain at least four points.");
            }

            return Create(() => new Polygon(outerRing));
        }).ToArray();

        return Create(() => new MultiPolygon(polygons));
    }

    private static GeometryCollection ReadGeometryCollectionGeometry(JsonElement element)
    {
        if (!TryGetProperty(element, "geometries", out var geometriesElement) || geometriesElement.ValueKind != JsonValueKind.Array)
        {
            throw Error("GeometryCollection requires geometries.");
        }

        var geometries = new List<GeoGeometry>();
        foreach (var geometryElement in geometriesElement.EnumerateArray())
        {
            geometries.Add(ReadGeometryObject(geometryElement));
        }

        if (geometries.Count == 0)
        {
            throw Error("A GeometryCollection must contain at least one geometry.");
        }

        return Create(() => new GeometryCollection(geometries));
    }

    private static IReadOnlyDictionary<string, object?> ReadProperties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw Error("Feature properties must be an object.");
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()) ?? new Dictionary<string, object?>();
    }

    private static GeoPoint ReadPosition(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Error("Coordinates must be an array.");
        }

        var coordinates = element.EnumerateArray().ToArray();
        if (coordinates.Length < 2)
        {
            throw Error("A point must contain at least two coordinates.");
        }

        if (coordinates.Any(coordinate => coordinate.ValueKind != JsonValueKind.Number))
        {
            throw Error("Coordinates must be numeric.");
        }

        var longitude = coordinates[0].GetDouble();
        var latitude = coordinates[1].GetDouble();
        return Create(() => new GeoPoint(latitude, longitude));
    }

    private static List<GeoPoint> ReadPositionList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Error("Coordinates must be an array.");
        }

        var points = new List<GeoPoint>();
        foreach (var positionElement in element.EnumerateArray())
        {
            points.Add(ReadPosition(positionElement));
        }

        return points;
    }

    private static List<List<GeoPoint>> ReadNestedPositionLists(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Error("Coordinates must be an array.");
        }

        var lineStrings = new List<List<GeoPoint>>();
        foreach (var lineStringElement in element.EnumerateArray())
        {
            lineStrings.Add(ReadPositionList(lineStringElement));
        }

        return lineStrings;
    }

    private static List<List<GeoPoint>> ReadPolygonLists(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Error("Coordinates must be an array.");
        }

        var polygons = new List<List<GeoPoint>>();
        foreach (var polygonElement in element.EnumerateArray())
        {
            var rings = ReadNestedPositionLists(polygonElement);
            if (rings.Count == 0)
            {
                throw Error("A Polygon must contain at least one ring.");
            }

            if (rings.Count != 1)
            {
                throw Error("A Polygon with holes is not supported.");
            }

            polygons.Add(rings[0]);
        }

        return polygons;
    }

    private static string ReadType(JsonElement element)
    {
        if (!TryGetProperty(element, "type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
        {
            throw Error("GeoJSON type is required.");
        }

        return typeElement.GetString() ?? throw Error("GeoJSON type is required.");
    }

    private static JsonElement RequireProperty(JsonElement element, string propertyName, string errorMessage)
    {
        if (!TryGetProperty(element, propertyName, out var propertyElement) || propertyElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw Error(errorMessage);
        }

        return propertyElement;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        property = default;
        return false;
    }

    private static JsonDocument ParseDocument(string geoJson)
    {
        if (geoJson is null)
        {
            throw Error("GeoJSON cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(geoJson))
        {
            throw Error("GeoJSON cannot be empty.");
        }

        try
        {
            return JsonDocument.Parse(geoJson);
        }
        catch (JsonException ex)
        {
            throw new GeoJsonException("Invalid GeoJSON.", ex);
        }
    }

    private static T Create<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (ArgumentException ex)
        {
            throw new GeoJsonException(ex.Message, ex);
        }
    }

    private static GeoJsonException Error(string message) => new(message);
}
