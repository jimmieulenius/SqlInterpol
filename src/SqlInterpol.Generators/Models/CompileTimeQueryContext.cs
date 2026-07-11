namespace SqlInterpol.Generators;

public class CompileTimeQueryContext
{
    public Dictionary<string, EntityDeclaration> Entities { get; } = new();
    public List<AppendCallContext> AppendCalls { get; } = new();
    
    // Tracks entities that are transformed into subqueries
    public HashSet<string> SubqueryEntities { get; } = new(); 
}