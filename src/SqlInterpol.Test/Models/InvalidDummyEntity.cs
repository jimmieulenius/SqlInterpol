using SqlInterpol.Config;
using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

public class InvalidDummyEntity : ISqlEntityBase 
{
    public ISqlReference this[string columnName] => throw new NotImplementedException();

    public ISqlReference Reference => throw new NotImplementedException();

    public ISqlDeclaration Declaration => throw new NotImplementedException();

    public ISqlFragment Column(string name) => throw new NotImplementedException();

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => throw new NotImplementedException();
}