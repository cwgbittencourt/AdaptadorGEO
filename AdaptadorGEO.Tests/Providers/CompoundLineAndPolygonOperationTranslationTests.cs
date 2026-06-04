using AdaptadorGEO;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class CompoundLineAndPolygonOperationTranslationTests
{
    [TestMethod]
    public void MultiLineString_intersects_translates_to_mysql_st_intersects()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(
            Geo.Column("shape").Intersects(
                Geo.MultiLineString(
                    Geo.LineString(Geo.Point(-23.55, -46.63), Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains(sql.CommandText, "ST_Intersects");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTILINESTRING");
    }

    [TestMethod]
    public void MultiPolygon_contains_translates_to_sqlserver_stcontains()
    {
        IGeoTranslator translator = new SqlServerSpatialTranslator();
        var sql = translator.Translate(
            Geo.Column("shape").Contains(
                Geo.MultiPolygon(
                    Geo.Polygon(
                        Geo.Point(-23.55, -46.63),
                        Geo.Point(-23.56, -46.64),
                        Geo.Point(-23.57, -46.65),
                        Geo.Point(-23.55, -46.63)))));

        StringAssert.Contains(sql.CommandText, "STContains");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTIPOLYGON");
    }

    [TestMethod]
    public void MultiPolygon_within_translates_to_postgis_st_within()
    {
        IGeoTranslator translator = new PostgreSqlSpatialTranslator();
        var sql = translator.Translate(
            Geo.Column("shape").Within(
                Geo.MultiPolygon(
                    Geo.Polygon(
                        Geo.Point(-23.55, -46.63),
                        Geo.Point(-23.56, -46.64),
                        Geo.Point(-23.57, -46.65),
                        Geo.Point(-23.55, -46.63)))));

        StringAssert.Contains(sql.CommandText, "ST_Within");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTIPOLYGON");
    }
}
