using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;
    private Span<SqlSegment> _segments;
    private int _segmentCount;
    private SqlSegment[] _arrayToReturn;

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _builder = builder;
        shouldAppend = true;

        int estimated = (literalLength / 10) + formattedCount + 2;
        _arrayToReturn = ArrayPool<SqlSegment>.Shared.Rent(Math.Max(estimated, 16));
        _segments = _arrayToReturn;
        _segmentCount = 0;
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        AddSegment(new SqlSegment(SqlSegmentType.Literal, value));
    }

    public void AppendFormatted<T>(T value, string? format = null, [CallerArgumentExpression("value")] string? expression = null)
    {
        if (value is ISqlFragment frag)
        {
            if (frag is SqlSegmentCollectionFragment collection)
            {
                string indent = "";
                if (_segmentCount > 0 && _segments[_segmentCount - 1].Type == SqlSegmentType.Literal)
                {
                    var prevText = _segments[_segmentCount - 1].Value?.ToString();
                    if (!string.IsNullOrEmpty(prevText))
                    {
                        int lastNewline = prevText.LastIndexOf('\n');
                        if (lastNewline >= 0)
                        {
                            int chars = 0;
                            int i = lastNewline + 1;
                            while (i < prevText.Length && (prevText[i] == ' ' || prevText[i] == '\t')) 
                            {
                                chars++;
                                i++;
                            }
                            if (chars > 0)
                            {
                                indent = prevText.Substring(lastNewline + 1, chars);
                            }
                        }
                    }
                }

                foreach (var segment in collection.Segments)
                {
                    if (indent.Length > 0 && segment.Type == SqlSegmentType.Literal && segment.Value is string s && s.Contains('\n'))
                    {
                        AddSegment(new SqlSegment(SqlSegmentType.Literal, s.Replace("\n", "\n" + indent), segment.RenderMode, segment.Tags));
                    }
                    else
                    {
                        AddSegment(segment);
                    }
                }
                return;
            }

            AddSegment(_builder.ProcessValue(frag));
            return;
        }

        if (!string.IsNullOrEmpty(expression))
        {
            int dotIndex = expression.IndexOf('.');

            if (dotIndex == -1)
            {
                if (_builder.ScopedVariables.TryGetValue(expression, out var tableEntity))
                {
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
                        else if (format == "decl" || (format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            // =================================================================
                            // FIX: Clean, strongly-typed alias assignment! No more reflection!
                            // =================================================================
                            if (string.IsNullOrEmpty(queryEntityBase.Reference.Alias) && queryEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                                aliasable.IsAliasQuoted = true; 
                            }
                            
                            var declFragment = new SqlSubqueryDeclarationFragment(queryEntity);
                            segment = _builder.ProcessValue(declFragment);
                        }
                        else
                        {
                            segment = _builder.ProcessValue((ISqlFragment)queryEntity);
                        }

                        AddSegment(segment);
                        return;
                    }

                    ISqlEntityBase? standardEntityBase = tableEntity as ISqlEntityBase;
                    if (tableEntity is ISqlDeclaration decl)
                    {
                        standardEntityBase = decl.Entity;
                    }

                    if (standardEntityBase != null)
                    {
                        SqlRenderMode? mode = format switch
                        {
                            "decl"  => SqlRenderMode.Declaration,
                            "alias" => SqlRenderMode.AliasOnly,
                            "base"  => SqlRenderMode.BaseName,
                            _       => null
                        };

                        if (format == "decl" || (format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            mode = SqlRenderMode.Declaration;
                            
                            // =================================================================
                            // FIX: Clean, strongly-typed alias assignment! No more reflection!
                            // =================================================================
                            if (string.IsNullOrEmpty(standardEntityBase.Reference.Alias) && standardEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                                aliasable.IsAliasQuoted = true;
                            }
                        }

                        var segmentResult = _builder.ProcessValue(tableEntity);
                        
                        if (mode != null)
                        {
                            segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode, segmentResult.Tags);
                        }
                        
                        AddSegment(segmentResult);
                        return;
                    }
                }
            }
            else if (dotIndex > 0 && expression.LastIndexOf('.') == dotIndex)
            {
                var varName = expression[..dotIndex];
                var propertyName = expression[(dotIndex + 1)..];

                if (_builder.ScopedVariables.TryGetValue(varName, out var entity))
                {
                    ISqlEntityBase? entityBase = entity as ISqlEntityBase;
                    if (entity is ISqlDeclaration decl)
                    {
                        entityBase = decl.Entity;
                    }

                    if (entityBase != null)
                    {
                        var meta = SqlMetadataRegistry.GetMetadata(entityBase.ModelType);
                        
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
        }

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