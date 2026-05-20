namespace SqlInterpol;

public interface ISqlDeclaration : ISqlFragment
{
    ISqlEntityBase Entity { get; }
}