namespace AdaptadorGEO.Spatial;

public sealed record GeoIntersectsExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;
