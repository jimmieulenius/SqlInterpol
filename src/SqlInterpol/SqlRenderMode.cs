namespace SqlInterpol;

public enum SqlRenderMode
{
    Default,
    AliasOnly,   // Renders just the alias/name: [stats]
    AsAlias,     // Renders as "AS alias": AS [stats]
    Declaration,
    BaseName
}