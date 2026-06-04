using AdaptadorGEO;
using AdaptadorGEO.Geometry;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Providers.PostgreSql;
using AdaptadorGEO.Providers.SqlServer;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class GeometryCollectionOperationTranslationTests
{
    [TestMethod]
    public void Geometry_collection_contains_translates_to_mysql_st_contains()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(Geo.Column("Area").Contains(
            Geo.GeometryCollection(
                Geo.Point(-23.55, -46.63),
                Geo.MultiPoint(Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains(sql.CommandText, "ST_Contains");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "GEOMETRYCOLLECTION");
    }

    [TestMethod]
    public void Geometry_collection_intersects_translates_to_sqlserver_stintersects()
    {
        IGeoTranslator translator = new SqlServerSpatialTranslator();
        var sql = translator.Translate(Geo.Column("Area").Intersects(
            Geo.GeometryCollection(
                Geo.Point(-23.55, -46.63),
                Geo.MultiPoint(Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains(sql.CommandText, "STIntersects");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "GEOMETRYCOLLECTION");
    }

    [TestMethod]
    public void Geometry_collection_within_translates_to_postgis_st_within()
    {
        IGeoTranslator translator = new PostgreSqlSpatialTranslator();
        var sql = translator.Translate(Geo.Column("Area").Within(
            Geo.GeometryCollection(
                Geo.Point(-23.55, -46.63),
                Geo.MultiPoint(Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains(sql.CommandText, "ST_Within");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "GEOMETRYCOLLECTION");
    }
}
