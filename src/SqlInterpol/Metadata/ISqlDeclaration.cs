namespace SqlInterpol.Metadata;

public interface ISqlDeclaration : ISqlFragment
{
    ISqlEntityBase Entity { get; }
}