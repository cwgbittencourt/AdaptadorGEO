using AdaptadorGEO.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.Common;

namespace AdaptadorGEO.Integration.EntityFrameworkCore;

public static class SqlFragmentEntityFrameworkExtensions
{
    public static DbParameter[] ToDbParameters(this SqlFragment fragment, DbConnection connection)
    {
        var command = connection.CreateCommand();

        return fragment.Parameters
            .Select(parameter =>
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Name;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                return dbParameter;
            })
            .ToArray();
    }

    public static Task<int> ExecuteSqlFragmentAsync(this DatabaseFacade database, SqlFragment fragment, CancellationToken cancellationToken = default)
    {
        var connection = database.GetDbConnection();
        var parameters = fragment.ToDbParameters(connection);

        return database.ExecuteSqlRawAsync(fragment.CommandText, parameters, cancellationToken);
    }
}
