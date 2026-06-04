using AdaptadorGEO;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class CompoundGeometryBufferTranslationTests
{
    [TestMethod]
    public void LineString_buffer_translates_to_mysql_st_buffer()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(
            Geo.Literal(
                Geo.LineString(
                    Geo.Point(-23.55, -46.63),
                    Geo.Point(-23.56, -46.64))).Buffer(250));

        StringAssert.Contains(sql.CommandText, "ST_Buffer");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "LINESTRING");
    }

    [TestMethod]
    public void Polygon_buffer_translates_to_sqlserver_stbuffer()
    {
        IGeoTranslator translator = new SqlServerSpatialTranslator();
        var sql = translator.Translate(
            Geo.Literal(
                Geo.Polygon(
                    Geo.Point(-23.55, -46.63),
                    Geo.Point(-23.56, -46.64),
                    Geo.Point(-23.57, -46.65),
                    Geo.Point(-23.55, -46.63))).Buffer(250));

        StringAssert.Contains(sql.CommandText, "STBuffer");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "POLYGON");
    }

    [TestMethod]
    public void GeometryCollection_buffer_translates_to_postgis_st_buffer()
    {
        IGeoTranslator translator = new PostgreSqlSpatialTranslator();
        var sql = translator.Translate(
            Geo.Literal(
                Geo.GeometryCollection(
                    Geo.Point(-23.55, -46.63),
                    Geo.MultiPoint(Geo.Point(-23.56, -46.64)))).Buffer(250));

        StringAssert.Contains(sql.CommandText, "ST_Buffer");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "GEOMETRYCOLLECTION");
    }
}
