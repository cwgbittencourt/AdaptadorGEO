using AdaptadorGEO.Geometry;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class PostgreSqlSpatialTranslatorTests
{
    [TestMethod]
    public void Intersects_translates_to_postgis_st_intersects()
    {
        IGeoTranslator translator = new PostgreSqlSpatialTranslator();
        var sql = translator.Translate(
            new GeoIntersectsExpression(
                new GeoColumn("Area"),
                new GeoLiteral(new GeoPoint(-23.55052, -46.63331))));

        StringAssert.Contains(sql.CommandText, "ST_Intersects");
    }
}
