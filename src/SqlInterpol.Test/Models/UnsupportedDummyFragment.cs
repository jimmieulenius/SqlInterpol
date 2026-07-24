using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Test.Models;

public class UnsupportedDummyFragment : ISqlFragment 
{ 
    public string ToSql(ISqlContext context, SqlRenderMode mode) => throw new NotImplementedException();
}