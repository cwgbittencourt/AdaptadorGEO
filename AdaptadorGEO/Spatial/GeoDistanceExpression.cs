namespace AdaptadorGEO.Spatial;

public sealed record GeoDistanceExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;
