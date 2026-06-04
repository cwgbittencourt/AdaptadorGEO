using AdaptadorGEO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AdaptadorGEO.Integration.EntityFrameworkCore;

public static class DatabaseFacadeGeoExtensions
{
    public static GeoDatabase AsGeoDatabase(this DatabaseFacade databaseFacade)
    {
        ArgumentNullException.ThrowIfNull(databaseFacade);
        return GeoDatabase.For(databaseFacade.GetDbConnection());
    }
}
