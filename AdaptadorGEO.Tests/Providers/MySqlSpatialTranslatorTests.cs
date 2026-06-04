using AdaptadorGEO.Geometry;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class MySqlSpatialTranslatorTests
{
    [TestMethod]
    public void Buffer_translates_to_mysql_st_buffer()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoBufferExpression(new GeoColumn("Location"), 250));

        StringAssert.Contains(sql.CommandText, "ST_Buffer");
        StringAssert.Contains(sql.CommandText, "@p0");
    }
}
