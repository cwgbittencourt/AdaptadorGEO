using AdaptadorGEO.Geometry;

namespace AdaptadorGEO.Spatial;

public sealed record GeoLiteral(GeoGeometry Value) : GeoExpression;
