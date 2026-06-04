using AdaptadorGEO;
using AdaptadorGEO.Providers.MySql;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class GeometryLiteralTranslationTests
{
    [TestMethod]
    public void MultiPoint_literal_translates_to_mysql_mult_point_wkt()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(Geo.MultiPoint(Geo.Point(-23.55, -46.63), Geo.Point(-23.56, -46.64))));

        StringAssert.Contains(sql.CommandText, "ST_GeomFromText");
        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTIPOINT");
    }

    [TestMethod]
    public void GeometryCollection_literal_translates_to_mysql_geometry_collection_wkt()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(
            Geo.GeometryCollection(
                Geo.Point(-23.55, -46.63),
                Geo.MultiPoint(Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains((string)sql.Parameters[0].Value!, "GEOMETRYCOLLECTION");
    }

    [TestMethod]
    public void MultiLineString_literal_translates_to_mysql_multi_line_string_wkt()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(
            Geo.MultiLineString(
                Geo.LineString(Geo.Point(-23.55, -46.63), Geo.Point(-23.56, -46.64)))));

        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTILINESTRING");
    }

    [TestMethod]
    public void MultiPolygon_literal_translates_to_mysql_multi_polygon_wkt()
    {
        IGeoTranslator translator = new MySqlSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(
            Geo.MultiPolygon(
                Geo.Polygon(
                    Geo.Point(-23.55, -46.63),
                    Geo.Point(-23.56, -46.64),
                    Geo.Point(-23.57, -46.65),
                    Geo.Point(-23.55, -46.63)))));

        StringAssert.Contains((string)sql.Parameters[0].Value!, "MULTIPOLYGON");
    }
}
