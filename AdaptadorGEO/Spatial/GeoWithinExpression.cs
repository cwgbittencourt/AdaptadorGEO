namespace AdaptadorGEO.Spatial;

public sealed record GeoWithinExpression(GeoExpression Left, GeoExpression Right) : GeoExpression;
