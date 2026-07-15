using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;
    
    // 🌟 ZERO-ALLOCATION BUCKET 🌟
    public struct PendingHole 
    {
        public bool IsLiteral;
        public string? StringValue; 
        public object? ObjectValue; 
        public string? Expression;  
    }
    
    private PendingHole[] _holes;
    private int _count;

    public SqlQueryInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder, out bool shouldAppend)
    {
        _builder = builder;
        shouldAppend = true;
        int estimated = (literalLength / 10) + formattedCount + 2;
        _holes = ArrayPool<PendingHole>.Shared.Rent(Math.Max(estimated, 16));
        _count = 0;
    }

    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (_count >= _holes.Length) GrowBuffer();
        _holes[_count++] = new PendingHole { IsLiteral = true, StringValue = value };
    }

    public void AppendFormatted<T>(T value, string? format = null, [CallerArgumentExpression("value")] string? expression = null)
    {
        if (_count >= _holes.Length) GrowBuffer();
        // 🌟 DEFERRED EXECUTION: Zero allocations here except for necessary value boxing! 🌟
        _holes[_count++] = new PendingHole { IsLiteral = false, ObjectValue = value, StringValue = format, Expression = expression };
    }

    private void GrowBuffer()
    {
        int newSize = _holes.Length * 2;
        var newArray = ArrayPool<PendingHole>.Shared.Rent(newSize);
        _holes.AsSpan(0, _count).CopyTo(newArray);
        ArrayPool<PendingHole>.Shared.Return(_holes);
        _holes = newArray;
    }

    /// <summary>
    /// Evaluates a specific formatted hole ON DEMAND. Used exclusively by the AOT Generator fallback.
    /// </summary>
    public SqlSegment GetSegment(int formattedHoleIndex)
    {
        int holeCount = 0;
        for (int i = 0; i < _count; i++)
        {
            ref var hole = ref _holes[i];
            if (!hole.IsLiteral)
            {
                if (holeCount == formattedHoleIndex)
                {
                    return EvaluateHole(ref hole);
                }
                holeCount++;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(formattedHoleIndex), "AOT requested a hole index that does not exist in the handler.");
    }

    /// <summary>
    /// Invoked by the JIT engine to lazily evaluate the AST only when AOT is not available.
    /// </summary>
    internal void TransferSegments(List<SqlSegment> destination)
    {
        for (int i = 0; i < _count; i++)
        {
            ref var hole = ref _holes[i];
            if (hole.IsLiteral)
            {
                destination.Add(new SqlSegment(SqlSegmentType.Literal, hole.StringValue!));
            }
            else
            {
                var segment = EvaluateHole(ref hole);
                
                // Formatting indentation
                if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlSegmentCollectionFragment collection)
                {
                    string indent = "";
                    if (destination.Count > 0 && destination[^1].Type == SqlSegmentType.Literal)
                    {
                        var prevText = destination[^1].Value?.ToString();
                        if (!string.IsNullOrEmpty(prevText))
                        {
                            int lastNewline = prevText.LastIndexOf('\n');
                            if (lastNewline >= 0)
                            {
                                int chars = 0;
                                int k = lastNewline + 1;
                                while (k < prevText.Length && (prevText[k] == ' ' || prevText[k] == '\t')) 
                                {
                                    chars++;
                                    k++;
                                }
                                if (chars > 0) indent = prevText.Substring(lastNewline + 1, chars);
                            }
                        }
                    }
                    foreach (var innerSeg in collection.Segments)
                    {
                        if (indent.Length > 0 && (innerSeg.Type == SqlSegmentType.Literal || innerSeg.Type == SqlSegmentType.Raw) && innerSeg.Value is string s && s.Contains('\n'))
                        {
                            destination.Add(new SqlSegment(innerSeg.Type, s.Replace("\n", "\n" + indent), innerSeg.RenderMode, innerSeg.Tags));
                        }
                        else
                        {
                            destination.Add(innerSeg);
                        }
                    }
                }
                else
                {
                    destination.Add(segment);
                }
            }
        }
        
        if (_holes != null)
        {
            ArrayPool<PendingHole>.Shared.Return(_holes);
            _holes = null!;
        }
    }

    // 🌟 AST AND REFLECTION ARE DEFERRED HERE 🌟
    private SqlSegment EvaluateHole(ref PendingHole hole)
    {
        var value = hole.ObjectValue;
        var format = hole.StringValue;
        var expression = hole.Expression;

        if (value is ISqlFragment frag)
        {
            return _builder.ProcessValue(frag);
        }

        if (!string.IsNullOrEmpty(expression))
        {
            int dotIndex = expression!.IndexOf('.');
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
                        return segment;
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
                        return segmentResult;
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
                        return segmentResult;
                    }
                }
            }
        }

        return _builder.ProcessValue(value);
    }
}