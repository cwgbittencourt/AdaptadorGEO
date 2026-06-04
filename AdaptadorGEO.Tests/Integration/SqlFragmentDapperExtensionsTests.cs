using AdaptadorGEO.Integration.Dapper;
using AdaptadorGEO.Sql;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdaptadorGEO.Tests.Integration;

[TestClass]
public class SqlFragmentDapperExtensionsTests
{
    [TestMethod]
    public void ToDynamicParameters_copies_sql_fragment_parameters()
    {
        var fragment = new SqlFragment(
            "select 1",
            new[]
            {
                new SqlParameter("@p0", 10),
                new SqlParameter("@p1", "abc")
            });

        DynamicParameters parameters = fragment.ToDynamicParameters();

        CollectionAssert.AreEquivalent(new[] { "p0", "p1" }, parameters.ParameterNames.ToArray());
        Assert.AreEqual(10, parameters.Get<int>("p0"));
        Assert.AreEqual("abc", parameters.Get<string>("p1"));
    }
}
