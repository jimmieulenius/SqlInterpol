using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;


public class SqlTheory
{
    public SqlTheory()
    {
    }

    public SqlTheory(Func<TheoryData<SqlTestCase>> dataProvider)
    {
        Data = dataProvider();
    }

    public required TheoryData<SqlTestCase> Data { get; init; }

    public bool AotIntercepted { get; init; } = true;
}