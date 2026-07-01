
using SqlInterpol.Parsing;

namespace SqlInterpol.Test.Models;

public class InvalidDummyEntity : ISqlEntityBase 
{
    public ISqlReference this[string columnName] => throw new NotImplementedException();

    public ISqlReference Reference => throw new NotImplementedException();

    public ISqlDeclaration Declaration => throw new NotImplementedException();

    public Type ModelType => throw new NotImplementedException();

    public SqlEntityRole Role => throw new NotImplementedException();

    public ISqlFragment Column(string name) => throw new NotImplementedException();

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => throw new NotImplementedException();
}