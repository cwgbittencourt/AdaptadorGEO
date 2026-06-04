using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Providers.SqlServer;

public sealed class SqlServerSpatialTranslator : IGeoTranslator
{
    public SqlFragment Translate(GeoExpression expression)
    {
        var parameters = new List<SqlParameter>();

        var commandText = expression switch
        {
            GeoBufferExpression buffer => $"{Render(buffer.Source, parameters)}.STBuffer({AddParameter(parameters, buffer.DistanceMeters)})",
            GeoIntersectsExpression intersects => $"{Render(intersects.Left, parameters)}.STIntersects({Render(intersects.Right, parameters)})",
            GeoContainsExpression contains => $"{Render(contains.Left, parameters)}.STContains({Render(contains.Right, parameters)})",
            GeoDistanceExpression distance => $"{Render(distance.Left, parameters)}.STDistance({Render(distance.Right, parameters)})",
            GeoWithinExpression within => $"{Render(within.Left, parameters)}.STWithin({Render(within.Right, parameters)})",
            GeoLiteral literal => $"geography::STGeomFromText({AddParameter(parameters, GeoWkt.Render(literal.Value))}, 4326)",
            GeoColumn column => $"[{column.Name}]",
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}")
        };

        return new SqlFragment(commandText, parameters);
    }

    private static string Render(GeoExpression expression, List<SqlParameter> parameters) =>
        expression switch
        {
            GeoColumn column => $"[{column.Name}]",
            GeoLiteral literal => $"geography::STGeomFromText({AddParameter(parameters, GeoWkt.Render(literal.Value))}, 4326)",
            _ => throw new NotSupportedException($"Unsupported nested expression: {expression.GetType().Name}")
        };

    private static string AddParameter(List<SqlParameter> parameters, object value)
    {
        var name = $"@p{parameters.Count}";
        parameters.Add(new SqlParameter(name, value));
        return name;
    }
}
