using SqlInterpol.Config;

namespace SqlInterpol;

public interface ISqlParameterGenerator
{
    void GenerateParameters(ISqlContext context);
}