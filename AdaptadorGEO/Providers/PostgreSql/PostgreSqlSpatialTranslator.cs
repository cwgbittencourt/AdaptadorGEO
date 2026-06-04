using AdaptadorGEO.Geometry;
using AdaptadorGEO.Sql;
using AdaptadorGEO.Spatial;

namespace AdaptadorGEO.Providers.PostgreSql;

public sealed class PostgreSqlSpatialTranslator : IGeoTranslator
{
    public SqlFragment Translate(GeoExpression expression)
    {
        var parameters = new List<SqlParameter>();

        var commandText = expression switch
        {
            GeoBufferExpression buffer => $"ST_Buffer({Render(buffer.Source, parameters)}, {AddParameter(parameters, buffer.DistanceMeters)})",
            GeoIntersectsExpression intersects => $"ST_Intersects({Render(intersects.Left, parameters)}, {Render(intersects.Right, parameters)})",
            GeoContainsExpression contains => $"ST_Contains({Render(contains.Left, parameters)}, {Render(contains.Right, parameters)})",
            GeoDistanceExpression distance => $"ST_Distance({Render(distance.Left, parameters)}, {Render(distance.Right, parameters)})",
            GeoWithinExpression within => $"ST_Within({Render(within.Left, parameters)}, {Render(within.Right, parameters)})",
            GeoLiteral literal => $"ST_GeomFromText({AddParameter(parameters, GeoWkt.Render(literal.Value))}, 4326)",
            GeoColumn column => $"\"{column.Name}\"",
            _ => throw new NotSupportedException($"Unsupported expression: {expression.GetType().Name}")
        };

        return new SqlFragment(commandText, parameters);
    }

    private static string Render(GeoExpression expression, List<SqlParameter> parameters) =>
        expression switch
        {
            GeoColumn column => $"\"{column.Name}\"",
            GeoLiteral literal => $"ST_GeomFromText({AddParameter(parameters, GeoWkt.Render(literal.Value))}, 4326)",
            _ => throw new NotSupportedException($"Unsupported nested expression: {expression.GetType().Name}")
        };

    private static string AddParameter(List<SqlParameter> parameters, object value)
    {
        var name = $"@p{parameters.Count}";
        parameters.Add(new SqlParameter(name, value));
        return name;
    }
}
