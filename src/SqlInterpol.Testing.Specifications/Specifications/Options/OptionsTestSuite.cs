using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IOptionsTestSuite))]
public abstract partial class OptionsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IOptionsTestSuite.CustomParameterIndexStartData))]
    public void Options_CustomParameterIndexStart(SqlTestCase testCase)
    {
        var options = new SqlInterpolOptions { ParameterIndexStart = 50 };
        var db = CreateBuilder(options);
        
        int val1 = 100;
        int val2 = 200;

        testCase.Act(() => db.Append($"SELECT * FROM Users WHERE Id = {val1} AND Age = {val2}").Build());
        testCase.Assert();
    }

    [SqlTest(nameof(IOptionsTestSuite.EnumFormattingData))]
    public void Options_GlobalEnumFormatting_AppliesToDtoMapping(SqlTestCase testCase)
    {
        // Arrange - Global override to force strings instead of integers
        var options = new SqlInterpolOptions { EnumFormat = SqlEnumFormat.String };
        var db = CreateBuilder(options);
        
        var dto = new StatusDto { Status = TestStatus.Active };

        // Act - We use a SET clause to trigger the DTO property mapper, which respects the global EnumFormat
        testCase.Act(() => 
        {
            db.Entity<StatusEntity>(out var e);
            return db.Append($"UPDATE {e} SET {dto}").Build();
        });

        // Assert
        testCase.Assert();
    }
}