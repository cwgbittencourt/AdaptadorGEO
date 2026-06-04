using AdaptadorGEO.Geometry;
using AdaptadorGEO.Providers.SqlServer;
using AdaptadorGEO.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Providers;

[TestClass]
public class SqlServerSpatialTranslatorTests
{
    [TestMethod]
    public void Point_literal_translates_to_sqlserver_geometry_or_geography_constructor()
    {
        IGeoTranslator translator = new SqlServerSpatialTranslator();
        var sql = translator.Translate(new GeoLiteral(new GeoPoint(-23.55052, -46.63331)));

        StringAssert.Contains(sql.CommandText, "STGeomFromText");
        StringAssert.Contains(sql.CommandText, "4326");
    }
}
