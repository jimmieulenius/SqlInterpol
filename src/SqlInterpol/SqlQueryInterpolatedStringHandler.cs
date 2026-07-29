using System.Buffers;
using System.Runtime.CompilerServices;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// A high-performance, zero-allocation interpolated string handler that defers evaluation
/// of interpolation holes until the builder requests them, enabling both the AOT interceptor
/// path (which calls <see cref="GetSegment"/>) and the JIT fallback path
/// (which calls <see cref="TransferSegments"/>).
/// </summary>
[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler
{
    private readonly SqlBuilder _builder;

    /// <summary>
    /// A deferred representation of a single literal or formatted hole in the interpolated string.
    /// Stored in a pooled array to avoid per-hole heap allocations.
    /// </summary>
    public struct PendingHole
    {
        /// <summary>Gets or sets a value indicating whether this slot holds a raw SQL literal.</summary>
        public bool IsLiteral;

        /// <summary>
        /// When <see cref="IsLiteral"/> is <see langword="true"/>, holds the literal text.
        /// When <see langword="false"/>, holds an optional format specifier (e.g., <c>"alias"</c>, <c>"decl"</c>).
        /// </summary>
        public string? StringValue;

        /// <summary>When <see cref="IsLiteral"/> is <see langword="false"/>, holds the interpolated value.</summary>
        public object? ObjectValue;

        /// <summary>
        /// The C# source expression captured via
        /// <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute"/>
        /// (e.g., <c>"p.Name"</c>), used for entity variable and property column resolution.
        /// </summary>
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
        // DEFERRED EXECUTION: Zero allocations here except for necessary value boxing!
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
    /// Retrieves the raw, unevaluated object passed into the interpolation hole.
    /// Used by the AOT compiler to safely extract schema metadata (like FallbackAlias)
    /// without triggering runtime column expansions or value mutations.
    /// </summary>
    public object? GetRawObject(int formattedHoleIndex)
    {
        int holeCount = 0;
        for (int i = 0; i < _count; i++)
        {
            ref var hole = ref _holes[i];
            if (!hole.IsLiteral)
            {
                if (holeCount == formattedHoleIndex)
                {
                    var value = hole.ObjectValue;
                    if (value is ISqlRenderModifier modifier)
                    {
                        return modifier.Value;
                    }
                    return value;
                }
                holeCount++;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(formattedHoleIndex), "AOT requested a hole index that does not exist in the handler.");
    }

    /// <summary>
    /// Evaluates a specific formatted hole on demand. Used exclusively by the AOT interceptor
    /// to access individual holes by index without iterating the full buffer.
    /// </summary>
    /// <param name="formattedHoleIndex">The zero-based index into the formatted (non-literal) holes only.</param>
    /// <returns>The evaluated <see cref="SqlSegment"/> for the requested hole.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="formattedHoleIndex"/> exceeds the number of formatted holes
    /// captured by this handler.
    /// </exception>
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
    /// Invoked by the JIT engine to lazily evaluate the full segment structure when AOT
    /// interception is not available. Transfers all accumulated holes into
    /// <paramref name="destination"/> and returns the pooled buffer to <see cref="System.Buffers.ArrayPool{T}"/>.
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
                    string indent = string.Empty;
                    if (destination.Count > 0)
                    {
                        var prev = destination[^1];
                        if (prev.Type == SqlSegmentType.Literal || prev.Type == SqlSegmentType.Raw)
                            indent = SqlIndentationHelper.ExtractTrailingLineIndent(prev.Value?.ToString());
                    }

                    foreach (var innerSeg in collection.Segments)
                    {
                        if (indent.Length > 0
                            && (innerSeg.Type == SqlSegmentType.Literal || innerSeg.Type == SqlSegmentType.Raw)
                            && innerSeg.Value is string s && s.Contains('\n'))
                        {
                            destination.Add(new SqlSegment(innerSeg.Type, SqlIndentationHelper.ApplyIndent(s, indent), innerSeg.RenderMode, innerSeg.Tags));
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

    // SEGMENT EVALUATION AND REFLECTION ARE DEFERRED HERE
    private SqlSegment EvaluateHole(ref PendingHole hole)
    {
        var value = hole.ObjectValue;
        var format = hole.StringValue;
        var expression = hole.Expression;
        SqlRenderMode? explicitMode = null;

        // Automatically unwrap strongly-typed render extensions
        if (value is ISqlRenderModifier modifier)
        {
            value = modifier.Value;
            explicitMode = modifier.Mode;
            
            if (!string.IsNullOrEmpty(modifier.OriginalExpression))
            {
                // The compiler's CallerArgumentExpression cleanly extracted the base (e.g. "p" or "p.Name")
                expression = modifier.OriginalExpression;
            }
            else if (!string.IsNullOrEmpty(expression))
            {
                // Fallback: compiler bug where it only captured the outer AppendFormatted expression.
                // We have "p.AsDeclaration()" or "p.Name.AsColumn()". We need to strip the method call.
                int lastDot = expression!.LastIndexOf('.');
                if (lastDot > 0)
                {
                    expression = expression.Substring(0, lastDot);
                }
            }
        }

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
                        var resolvedMode = explicitMode ?? format switch
                        {
                            "alias" => SqlRenderMode.AliasOnly,
                            "base"  => SqlRenderMode.BaseName,
                            "decl"  => SqlRenderMode.Declaration,
                            _       => null
                        };

                        if (resolvedMode == SqlRenderMode.AliasOnly || resolvedMode == SqlRenderMode.BaseName)
                        {
                            var refSegment = _builder.ProcessValue(queryEntityBase.Reference);
                            return new SqlSegment(refSegment.Type, refSegment.Value, resolvedMode, refSegment.Tags);
                        }
                        else if (resolvedMode == SqlRenderMode.Declaration || (resolvedMode == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            if (string.IsNullOrEmpty(queryEntityBase.Reference.Alias) && queryEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                                aliasable.IsAliasQuoted = true;
                            }
                            var declFragment = new SqlSubqueryDeclarationFragment(queryEntity);
                            return _builder.ProcessValue(declFragment);
                        }
                        
                        return _builder.ProcessValue((ISqlFragment)queryEntity);
                    }
                    
                    ISqlEntityBase? standardEntityBase = tableEntity as ISqlEntityBase;
                    if (tableEntity is ISqlDeclaration decl)
                    {
                        standardEntityBase = decl.Entity;
                    }

                    if (standardEntityBase != null)
                    {
                        SqlRenderMode? mode = explicitMode ?? format switch
                        {
                            "decl"  => SqlRenderMode.Declaration,
                            "alias" => SqlRenderMode.AliasOnly,
                            "base"  => SqlRenderMode.BaseName,
                            _       => null
                        };
                        
                        if (mode == SqlRenderMode.Declaration || (mode == null && format == null && _builder.Context.Options.EntityAutoAliasing))
                        {
                            mode = SqlRenderMode.Declaration;
                            if (string.IsNullOrEmpty(standardEntityBase.Reference.Alias) && standardEntityBase.Reference is ISqlAliasable aliasable)
                            {
                                aliasable.Alias = expression;
                                aliasable.IsAliasQuoted = true;
                            }
                        }

                        // PREVENT JIT FROM EXPANDING INTO COLUMNS FOR ALIAS/BASE EXPLICIT MODES
                        if (mode == SqlRenderMode.AliasOnly || mode == SqlRenderMode.BaseName)
                        {
                            var refSegment = _builder.ProcessValue(standardEntityBase.Reference);
                            return new SqlSegment(refSegment.Type, refSegment.Value, mode.Value, refSegment.Tags);
                        }

                        // FULL ENTITY PASS-THROUGH FOR DECLARATIONS AND DEFAULTS 
                        // (Allows SqlBuilder to natively detect DML context and safely strip auto-aliases)
                        var segmentResult = _builder.ProcessValue(tableEntity);
                        if (mode != null)
                        {
                            segmentResult = new SqlSegment(segmentResult.Type, segmentResult.Value, mode.Value, segmentResult.Tags);
                        }
                        return segmentResult;
                    }
                }
            }
            else if (dotIndex > 0 && expression.LastIndexOf('.') == dotIndex)
            {
                var varName = expression.Substring(0, dotIndex);
                var propertyName = expression.Substring(dotIndex + 1);

                if (_builder.ScopedVariables.TryGetValue(varName, out var entity))
                {
                    ISqlEntityBase? entityBase = entity as ISqlEntityBase;
                    if (entity is ISqlDeclaration innerDecl)
                    {
                        entityBase = innerDecl.Entity;
                    }

                    if (entityBase != null)
                    {
                        var meta = SqlMetadataRegistry.GetMetadata(entityBase.ModelType);
                        var memberMeta = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
                        string physicalColumnName = memberMeta != null ? meta.Columns[memberMeta] : propertyName;

                        var columnRef = new SqlColumnReference(entityBase.Reference, physicalColumnName, propertyName);

                        SqlRenderMode? mode = explicitMode ?? format switch
                        {
                            "col"   => SqlRenderMode.BaseName,
                            "alias" => SqlRenderMode.AliasOnly,
                            "base"  => SqlRenderMode.BaseName,
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