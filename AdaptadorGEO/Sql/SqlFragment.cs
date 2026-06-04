namespace AdaptadorGEO.Sql;

public sealed record SqlFragment(string CommandText, IReadOnlyList<SqlParameter> Parameters);
