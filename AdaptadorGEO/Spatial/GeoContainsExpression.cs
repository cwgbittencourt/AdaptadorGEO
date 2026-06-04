namespace AdaptadorGEO.Spatial;

public sealed record GeoContainsExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;
