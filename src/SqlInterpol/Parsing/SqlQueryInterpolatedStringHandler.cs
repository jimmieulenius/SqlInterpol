using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

/// <summary>
/// The interpolated string handler for <see cref="SqlBuilder"/> that collects raw SQL literals
/// and interpolated values into a pooled segment buffer during query construction.
/// </summary>
[InterpolatedStringHandler]
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

    /// <summary>Appends a raw SQL literal string as a pure literal segment.</summary>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        
        // NO MORE PARSER: Just box the raw text!
        AddSegment(new SqlSegment(SqlSegmentType.Literal, value));
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
                    if (tableEntity is ISqlQuery queryEntity && tableEntity is ISqlEntityBase queryEntityBase)
                    {
                        SqlSegment segment;

                        if (format == "alias")
                        {
                            segment = _builder.ProcessValue(queryEntityBase.Reference);
                            segment = new SqlSegment(segment.Type, segment.Value, SqlRenderMode.AliasOnly, segment.Tags);
                        }
                        else if (format == "base")
                        {
                            segment = _builder.ProcessValue(queryEntityBase.Reference);
                            segment = new SqlSegment(segment.Type, segment.Value, SqlRenderMode.BaseName, segment.Tags);
                        }
                        // Handle explicit :decl OR naked variable invocation when EntityAutoAliasing is true
                        else if (format == "decl" || (format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            // FIX: Auto-inject the C# variable name as the SQL alias!
                            if (string.IsNullOrEmpty(queryEntityBase.Reference.Alias) && queryEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                            }
                            
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
                    if (tableEntity is ISqlEntityBase standardEntityBase)
                    {
                        SqlRenderMode? mode = format switch
                        {
                            "decl"  => SqlRenderMode.Declaration,
                            "alias" => SqlRenderMode.AliasOnly,
                            "base"  => SqlRenderMode.BaseName,
                            _       => null
                        };

                        // FIX: Auto-inject the C# variable name as the SQL alias!
                        if (format == "decl" || (format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            mode = SqlRenderMode.Declaration;
                            if (string.IsNullOrEmpty(standardEntityBase.Reference.Alias) && standardEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                            }
                        }

                        var segmentResult = _builder.ProcessValue(standardEntityBase);
                        
                        if (mode != null)
                        {
                            segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode, segmentResult.Tags);
                        }
                        
                        AddSegment(segmentResult);
                        return;
                    }
                }
            }
            // Scenario B: Column projection (e.g., SELECT {p.Id} or {stats.TotalPrice:col})
            else if (dotIndex > 0 && expression.LastIndexOf('.') == dotIndex)
            {
                var varName = expression[..dotIndex];
                var propertyName = expression[(dotIndex + 1)..];

                if (_builder.ScopedVariables.TryGetValue(varName, out var entity) && entity is ISqlEntityBase entityBase)
                {
                    // FIX: Lightning fast O(1) type resolution replacing slow array reflection
                    var meta = SqlMetadataRegistry.GetMetadata(entityBase.ModelType);
                    
                    // Optimized column mapping lookup sequence matching our preprocessor
                    var memberMeta = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                    string physicalColumnName = memberMeta != null ? meta.Columns[memberMeta] : propertyName;

                    var columnRef = new SqlColumnReference(entityBase.Reference, physicalColumnName, propertyName);
                    
                    SqlRenderMode? mode = format switch
                    {
                        "col"   => SqlRenderMode.BaseName,
                        "alias" => SqlRenderMode.AliasOnly,
                        _       => null
                    };

                    var segmentResult = _builder.ProcessValue(columnRef);
                    if (mode != null)
                    {
                        segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode, segmentResult.Tags);
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