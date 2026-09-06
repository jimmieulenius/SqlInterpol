# Compiler Pipeline, Rewriters & Rendering

When you build a query, the `SqlPipeline` orchestrates preprocessing and segment rewriting before delegating to the dialect's final renderer. 

## Segments vs. Fragments

Instead of relying on an abstract syntax tree, the engine builds queries using a flat, tokenized timeline of segments and renderable fragments:

*   **`SqlSegment`**: Represents a single tokenized piece of the SQL query timeline. Segments hold contextual data—such as raw text, parameters, or unresolved objects—and can be annotated with semantic tags for easy identification by the pipeline.
*   **`ISqlFragment`**: Represents any SQL construct that can render itself to a SQL string given a dialect context. Fragments define how structural pieces (like entities, columns, or clauses) output SQL via their `ToSql()` method.

### Creating a Custom Fragment

If you need a reusable SQL construct that correctly quotes identifiers, parametrizes values, and formats itself based on the active dialect, you can implement your own `ISqlFragment`. 

Here is an example of a fragment that enforces multi-tenant isolation by safely parameterizing a `TenantId` against a specific table reference:

```csharp
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

public class TenantFilterFragment(ISqlReference tableReference, int tenantId) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. Resolve the table's alias (or fallback to its base name)
        string tableRef = tableReference.ToSql(context, SqlRenderMode.AliasOnly);
        if (string.IsNullOrWhiteSpace(tableRef))
        {
            tableRef = tableReference.ToSql(context, SqlRenderMode.BaseName);
        }

        // 2. Safely quote the column name using the active dialect
        string tenantCol = context.Dialect.QuoteIdentifier("TenantId");
        
        // 3. Register the raw value into the query's parameter collection
        string paramName = context.AddParameter(tenantId);

        return $"{tableRef}.{tenantCol} = {paramName}";
    }
}
```

To insert this fragment into the query timeline, you wrap it in a `SqlSegment` with the type `SqlSegmentType.Raw` (or `SqlSegmentType.Fragment`) so the pipeline knows to invoke its `ToSql()` method during rendering. In this example, `42` represents the dynamic `tenantId` pulled from the application's security or request context:

```csharp
// Example of wrapping the fragment into a pipeline segment
int currentTenantId = 42; // In reality, pulled from context
var segment = new SqlSegment(SqlSegmentType.Raw, new TenantFilterFragment(entity.Reference, currentTenantId));
```

## The Compilation Pipeline

### 1. Preprocessor & Rules

The **`ISqlSegmentPreprocessor`** acts as a lexical and semantic processor that transforms, resolves, and tags a raw stream of SQL segments. It evaluates unresolved values, generates database parameters, and isolates structural DML keywords based on the query's context.

You can inject custom lexical analysis rules by implementing **`ISqlPreprocessorRule`**. These rules process segments sequentially and can read or modify the mutable `SqlPreprocessorState` to intercept specific tokens. 

For example, to intercept a proprietary macro (`$$NOW$$`) and replace it with a dialect-specific function:

```csharp
using System.Collections.Generic;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

public class LegacyTimeMacroRule : ISqlPreprocessorRule
{
    public bool Process(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state)
    {
        // Intercept string literals to find our custom macro
        if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
        {
            if (text.Contains("$$NOW$$"))
            {
                // Modify the text and push it to the state's refined list
                string replaced = text.Replace("$$NOW$$", "GET_LEGACY_DATE()");
                state.Refined.Add(new SqlSegment(SqlSegmentType.Literal, replaced, segment.RenderMode, segment.Tags));
                
                // Return true to tell the core pipeline this segment is fully handled
                return true; 
            }
        }
        return false;
    }
}
```

### 2. Segment Rewriters

**`ISqlSegmentRewriter`** instances perform fast, discrete transformations on the segment timeline. Rewriters use a read-only `ISqlPipelineState` to check their applicability in $O(1)$ time by checking for tags. If applicable, the rewriter applies structural transformations.

For example, this rewriter guarantees that every `FROM` clause gets a `WITH (NOLOCK)` hint appended to the table reference:

```csharp
using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

public class LegacyNoLockRewriter : ISqlSegmentRewriter
{
    // O(1) check: Only execute if the parser detected a FROM clause anywhere in the query
    public bool IsApplicable(ISqlPipelineState state) => state.HasTag(SqlSegmentTag.FromKeyword);

    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count + 2);

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            rewritten.Add(segment);

            // If we detect a table declaration following a FROM keyword, inject the lock hint
            if (segment.Type == SqlSegmentType.Reference && i > 0)
            {
                if (segments[i - 1].HasTag(SqlSegmentTag.FromKeyword) || 
                    segments[i - 1].HasTag(SqlSegmentTag.JoinKeyword))
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " WITH (NOLOCK)"));
                }
            }
        }
        
        return rewritten;
    }
}
```

### 3. Renderers

The **`ISqlSegmentRenderer`** is the final step, responsible for converting individual `SqlSegment` instances to their finalized SQL strings. You can supply a custom renderer to override how specific segment types are emitted.

> ℹ️ **Packaging custom rules and rewriters as a plugin**  
> To bundle `ISqlPreprocessorRule`, `ISqlSegmentRewriter`, and renderer overrides into a distributable extension that applies automatically, implement `ISqlExtension` and register it globally. See [Extensibility & Dialects](extensibility-dialects.md).