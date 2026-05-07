namespace SqlInterpol;

public interface ISqlQuery : ISqlFragment
{
    IReadOnlyList<SqlSegment> Segments { get; }
    
    SqlQueryResult Build();
}

public interface ISqlQuery<T> : ISqlQuery, ISqlEntityBase<T>
{
}