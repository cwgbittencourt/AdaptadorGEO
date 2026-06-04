using AdaptadorGEO.Sql;
using Dapper;
using System.Data;

namespace AdaptadorGEO.Integration.Dapper;

public static class SqlFragmentDapperExtensions
{
    public static DynamicParameters ToDynamicParameters(this SqlFragment fragment)
    {
        var parameters = new DynamicParameters();

        foreach (var parameter in fragment.Parameters)
        {
            parameters.Add(NormalizeName(parameter.Name), parameter.Value);
        }

        return parameters;
    }

    public static int ExecuteSqlFragment(this IDbConnection connection, SqlFragment fragment, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        return connection.Execute(fragment.CommandText, fragment.ToDynamicParameters(), transaction, commandTimeout);
    }

    public static IEnumerable<T> QuerySqlFragment<T>(this IDbConnection connection, SqlFragment fragment, IDbTransaction? transaction = null, int? commandTimeout = null)
    {
        return connection.Query<T>(fragment.CommandText, fragment.ToDynamicParameters(), transaction, true, commandTimeout);
    }

    private static string NormalizeName(string name) => name.TrimStart('@', ':', '?');
}
