using AdaptadorGEO.Sql;

namespace AdaptadorGEO.Spatial;

public interface IGeoTranslator
{
    SqlFragment Translate(GeoExpression expression);
}
