namespace SqlInterpol.Abstractions;

public interface ISqlDeclaration : ISqlFragment
{
    ISqlReference Reference { get; }
}