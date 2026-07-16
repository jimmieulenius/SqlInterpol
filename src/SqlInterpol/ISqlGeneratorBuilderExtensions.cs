using System.Runtime.CompilerServices;
using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Highly optimized runtime helpers designed exclusively to be called by the AOT Source Generator.
/// </summary>
public static class ISqlGeneratorBuilderExtensions
{
    /// <summary>
    /// Dynamically applies the runtime formatting preferences (Vertical vs Horizontal, Indentation)
    /// without requiring allocated fragment objects.
    /// </summary>
    /// <param name="builder">The generator builder instance.</param>
    /// <param name="isNewLine">Indicates if the formatting should inject a line break.</param>
    /// <param name="indentLevel">The number of indentation levels to apply if vertical formatting is active.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendFormatting(this ISqlGeneratorBuilder builder, bool isNewLine, int indentLevel = 0)
    {
        var options = builder.Context.Options;
        
        if (options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            if (isNewLine) 
                builder.AppendRaw("\n");
                
            if (indentLevel > 0) 
                builder.AppendRaw(new string(' ', options.IndentSize * indentLevel));
        }
        else if (isNewLine)
        {
            builder.AppendRaw(" ");
        }
    }

    /// <summary>
    /// Rapidly emits a mapped <c>INSERT VALUES</c> clause for anonymous types using AOT-resolved columns 
    /// and pre-fetched dynamic properties.
    /// </summary>
    /// <param name="builder">The generator builder instance.</param>
    /// <param name="columns">The array of database column names to insert into.</param>
    /// <param name="values">The array of corresponding values to parameterize.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendInsertValues(this ISqlGeneratorBuilder builder, string[] columns, object?[] values)
    {
        var dialect = builder.Context.Dialect;
        
        // 1. Column Declaration
        builder.AppendFormatting(isNewLine: true);
        builder.AppendRaw("(");
        builder.AppendFormatting(isNewLine: true, indentLevel: 1);
        
        for (int i = 0; i < columns.Length; i++)
        {
            builder.AppendRaw(dialect.QuoteIdentifier(columns[i]));
            if (i < columns.Length - 1)
            {
                builder.AppendRaw(",");
                builder.AppendFormatting(isNewLine: true, indentLevel: 1);
            }
        }
        
        builder.AppendFormatting(isNewLine: true);
        builder.AppendRaw(")");
        
        // 2. VALUES Clause
        builder.AppendFormatting(isNewLine: true);
        builder.AppendRaw("VALUES", SqlSegmentTag.InsertValuesKeyword);
        builder.AppendFormatting(isNewLine: true);
        builder.AppendRaw("(");
        builder.AppendFormatting(isNewLine: true, indentLevel: 1);
        
        for (int i = 0; i < values.Length; i++)
        {
            builder.AppendSegment(new SqlSegment(SqlSegmentType.Parameter, values[i]));
            if (i < values.Length - 1)
            {
                builder.AppendRaw(",");
                builder.AppendFormatting(isNewLine: true, indentLevel: 1);
            }
        }
        builder.AppendFormatting(isNewLine: true);
        builder.AppendRaw(")");
    }
}