namespace SqlInterpol.Pipeline;

/// <summary>
/// A specialized collection for SQL segment rewriters that ensures pipeline stability 
/// by preventing duplicate rewriter types from being registered.
/// </summary>
public class SqlSegmentRewriterCollection : UniqueCollection<ISqlSegmentRewriter>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlSegmentRewriterCollection"/> class,
    /// enforcing uniqueness by the exact <see cref="Type"/> of the rewriter.
    /// </summary>
    public SqlSegmentRewriterCollection() : base(rewriter => rewriter.GetType())
    {
    }

    /// <summary>
    /// Removes a rewriter of the specified type from the pipeline.
    /// </summary>
    /// <typeparam name="TRewriter">The exact type of the rewriter to remove.</typeparam>
    public void Remove<TRewriter>() where TRewriter : ISqlSegmentRewriter
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i].GetType() == typeof(TRewriter))
            {
                RemoveAt(i);
                return;
            }
        }
    }
}