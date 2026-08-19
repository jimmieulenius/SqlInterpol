using SqlInterpol.Configuration;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IParametersTestSuite))]
public abstract partial class ParametersTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IParametersTestSuite.SingleParameterData))]
    public void Parameters_ImplicitPrimitiveValues(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int id = 42;
        string name = "Test";

        testCase.Act(() => db.Append($"SELECT * FROM Users WHERE Id = {id} AND Name = {name}").Build());
        testCase.Assert();
    }

    [SqlTest(nameof(IParametersTestSuite.NullParameterData))]
    public void Parameters_NullValue(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        string? name = null;

        testCase.Act(() => db.Append($"SELECT * FROM Users WHERE Name = {name}").Build());
        testCase.Assert();
    }

    [SqlTest(nameof(IParametersTestSuite.CollectionParameterData))]
    public void Parameters_CollectionValue_ExpandsToInClause(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int[] ids = [1, 2, 3];

        // FIX: Wrap the collection hole in parentheses so the expanded list is valid SQL!
        testCase.Act(() => db.Append($"SELECT * FROM Users WHERE Id IN ({ids})").Build());
        testCase.Assert();
    }

    [SqlTest(nameof(IParametersTestSuite.ExplicitSqlArgData))]
    public void Parameters_ExplicitSqlArg(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        string name = "Alice";

        // FIX: Sql.Arg("paramName") binds the hole to an argument passed into Build()!
        testCase.Act(() => db.Append($"SELECT * FROM Users WHERE Name = {Sql.Arg("userName")}").Build(new { userName = name }));
        testCase.Assert();
    }
}