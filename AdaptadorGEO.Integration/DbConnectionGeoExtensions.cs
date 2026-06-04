using AdaptadorGEO;
using System.Data.Common;

namespace AdaptadorGEO.Integration;

public static class DbConnectionGeoExtensions
{
    public static GeoDatabase AsGeoDatabase(this DbConnection connection) => GeoDatabase.For(connection);
}
