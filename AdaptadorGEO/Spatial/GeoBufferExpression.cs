namespace AdaptadorGEO.Spatial;

public sealed record GeoBufferExpression(GeoExpression Source, double DistanceMeters) : GeoExpression;
