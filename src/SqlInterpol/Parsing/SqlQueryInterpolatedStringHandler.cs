using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

internal class SqlSubqueryDeclarationFragment : ISqlFragment
{
    private readonly ISqlQuery _query;

    public SqlSubqueryDeclarationFragment(ISqlQuery query)
    {
        _query = query;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. Extract the pristine inner SELECT statement layout
        string innerSql = ((ISqlFragment)_query).ToSql(context, mode);
        
        // 2. Discover the baseline indent of the current line by looking backward 
        // at the trailing text of the preceding literal segment relative to the active rendering cursor.
        string baseIndent = "";
        if (context is SqlBuilder builder)
        {
            int currentIndex = ((ISqlParserState)context).CurrentSegmentIndex;
            
            if (currentIndex > 0 && currentIndex - 1 < builder.Segments.Count)
            {
                var lastSeg = builder.Segments[currentIndex - 1];
                if (lastSeg.Value is string lastLiteral)
                {
                    int lastNewLine = lastLiteral.LastIndexOf('\n');
                    string currentLinePrefix = lastNewLine >= 0 ? lastLiteral[(lastNewLine + 1)..] : lastLiteral;
                    
                    // Capture all preceding spaces/tabs on the current active line
                    baseIndent = new string(currentLinePrefix.TakeWhile(char.IsWhiteSpace).ToArray());
                }
            }
        }

        // 3. Apply the golden layout rule: Inner Level Indent = Current Baseline + Configured Indent Size
        string extraIndent = new string(' ', context.Options.IndentSize);
        string totalBodyIndent = baseIndent + extraIndent;

        // 4. Shift all lines of the inner body cleanly into the calculated indentation track
        string[] lines = innerSql.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string formattedInnerSql = string.Join("\n", lines.Select(l => string.IsNullOrWhiteSpace(l) ? l : totalBodyIndent + l));
        
        // 5. Resolve and cleanly quote the auto-captured C# variable name as the SQL alias
        var entityRef = ((ISqlEntityBase)_query).Reference;
        string alias = entityRef.Alias ?? entityRef.FallbackAlias ?? "stats";
        string quotedAlias = context.Dialect.QuoteIdentifier(alias);
        
        // 6. Package it up, aligning the closing parenthesis perfectly back with the parent line's baseline!
        return $"(\n{formattedInnerSql}\n{baseIndent}) AS {quotedAlias}";
    }
}

[InterpolatedStringHandler]
/// <summary>
/// The interpolated string handler for <see cref="SqlBuilder"/> that collects raw SQL literals
/// and interpolated values into a pooled segment buffer during query construction.
/// </summary>
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;
    private Span<SqlSegment> _segments;
    private int _segmentCount;
    private SqlSegment[] _arrayToReturn;

    /// <summary>
    /// Initializes the handler with a pooled segment buffer sized to the estimated number of segments.
    /// </summary>
    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _builder = builder;
        shouldAppend = true;

        int estimated = (literalLength / 10) + formattedCount + 2;
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimated, 16));
        _segments = _arrayToReturn;
        _segmentCount = 0;
    }

    /// <summary>Appends a raw SQL literal string as a processed segment.</summary>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AddSegment(_builder.ProcessLiteral(value));
    }

    /// <summary>
    /// Appends an interpolated value as a processed segment, capturing the C# expression string
    /// to automatically map lambda variables to SQL entities and columns without string allocations.
    /// </summary>
    public void AppendFormatted<T>(T value, string? format = null, [CallerArgumentExpression("value")] string? expression = null)
    {
        // 1. PRESERVE OLD SYNTAX: Process explicit SQL fragments normally
        if (value is ISqlFragment frag)
        {
            AddSegment(_builder.ProcessValue(frag));
            return;
        }

        // 2. NEW SYNTAX: AST Routing via CallerArgumentExpression
        if (!string.IsNullOrEmpty(expression))
        {
            int dotIndex = expression.IndexOf('.');

            // Scenario A: Direct POCO or Query access (e.g., FROM {p} or {stats})
            if (dotIndex == -1)
            {
                if (_builder.ScopedVariables.TryGetValue(expression, out var tableEntity))
                {
                    // SPECIALIZED SUBQUERY ROUTING
                    if (tableEntity is ISqlQuery queryEntity)
                    {
                        SqlSegment segment;
                        var entityBase = (ISqlEntityBase)queryEntity;

                        if (format == "alias")
                        {
                            segment = _builder.ProcessValue(entityBase.Reference);
                            segment = new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tag);
                        }
                        else if (format == "base")
                        {
                            segment = _builder.ProcessValue(entityBase.Reference);
                            segment = new SqlSegment(segment.Type, segment.Value, SqlRenderMode.BaseName, segment.Tag);
                        }
                        // Handle explicit :decl OR naked variable invocation when EntityAutoAliasing is true
                        else if (format == "decl" || (format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            var declFragment = new SqlSubqueryDeclarationFragment(queryEntity);
                            segment = _builder.ProcessValue(declFragment);
                        }
                        else
                        {
                            // Manual formatting fallback: User wrote the parenthesis themselves.
                            // Cast directly to ISqlFragment to bypass auto-indentation and print raw layouts.
                            segment = _builder.ProcessValue((ISqlFragment)queryEntity);
                        }

                        AddSegment(segment);
                        return;
                    }

                    // STANDARD PHYSICAL TABLE ROUTING
                    SqlRenderMode? mode = format switch
                    {
                        "decl"  => SqlRenderMode.Declaration,
                        "alias" => SqlRenderMode.AliasOnly,
                        "base"  => SqlRenderMode.BaseName,
                        _       => null
                    };

                    var segmentResult = _builder.ProcessValue(tableEntity);
                    
                    if (mode != null)
                    {
                        segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode, segmentResult.Tag);
                    }
                    
                    AddSegment(segmentResult);
                    return;
                }
            }
            // Scenario B: Column projection (e.g., SELECT {p.Id} or {stats.TotalPrice:col})
            else if (dotIndex > 0 && expression.LastIndexOf('.') == dotIndex)
            {
                var varName = expression[..dotIndex];
                var propertyName = expression[(dotIndex + 1)..];

                if (_builder.ScopedVariables.TryGetValue(varName, out var entity))
                {
                    var entityModelType = entity.GetType().GetGenericArguments()[0];
                    var meta = SqlMetadataRegistry.GetMetadata(entityModelType);
                    
                    var columnMap = meta.Columns.FirstOrDefault(c => c.Key.Name == propertyName);
                    string physicalColumnName = columnMap.Key != null ? columnMap.Value : propertyName;

                    var columnRef = new SqlColumnReference(entity.Reference, physicalColumnName, propertyName);
                    
                    SqlRenderMode? mode = format switch
                    {
                        "col"   => SqlRenderMode.BaseName,
                        "alias" => SqlRenderMode.AliasOnly,
                        _       => null
                    };

                    var segmentResult = _builder.ProcessValue(columnRef);
                    if (mode != null)
                    {
                        segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode, segmentResult.Tag);
                    }
                    AddSegment(segmentResult);
                    return;
                }
            }
        }

        // 3. Fallback for standard parameters and iterables
        AddSegment(_builder.ProcessValue(value));
    }

    private void AddSegment(SqlSegment segment)
    {
        if (_segmentCount >= _segments.Length) GrowBuffer();
        _segments[_segmentCount++] = segment;
    }

    private void GrowBuffer()
    {
        int newSize = _segments.Length * 2;
        var newArray = ArrayPool<SqlSegment>.Shared.Rent(newSize);
        _segments[.._segmentCount].CopyTo(newArray);
        if (_arrayToReturn != null) ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
        _arrayToReturn = newArray;
        _segments = _arrayToReturn;
    }

    /// <summary>Transfers all collected segments into <paramref name="destination"/> and releases the pooled buffer.</summary>
    internal void TransferSegments(List<SqlSegment> destination)
    {
        for (int i = 0; i < _segmentCount; i++) destination.Add(_segments[i]);

        if (_arrayToReturn != null)
        {
            ArrayPool<SqlSegment>.Shared.Return(_arrayToReturn);
            _arrayToReturn = null!;
        }
    }
}