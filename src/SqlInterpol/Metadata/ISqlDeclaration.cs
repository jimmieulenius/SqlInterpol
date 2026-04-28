namespace SqlInterpol.Metadata;

public interface ISqlDeclaration : ISqlFragment
{
    ISqlReference Reference { get; }
}