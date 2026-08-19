using System.Collections.Concurrent;
using System.Text;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Execution;

/// <summary>
/// Globally caches auto-generated, dialect-specific CRUD templates.
/// </summary>
/// <remarks>
/// All four operations (INSERT, UPDATE, DELETE, UPSERT) share the same generation pattern:
/// build typed segment streams, run them through the dialect's rewriter pipeline using a
/// <see cref="SqlTemplateContext"/>, and cache the resulting format strings.
/// A dialect author can customize any CRUD operation simply by adding a rewriter override —
/// no changes to this cache are ever required.
/// </remarks>
internal static class SqlCrudTemplateCache
{
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind), ISqlTemplate> _insertCache = new();
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind, string), ISqlTemplate> _updateCache = new();
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind, string), ISqlTemplate> _deleteCache = new();
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind, string), ISqlTemplate> _upsertCache = new();

    private static Type UnwrapType(Type type)
    {
        if (type == typeof(string)) return type;
        if (type.IsArray) return type.GetElementType()!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return type.GetGenericArguments()[0];
        
        var ienum = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (ienum != null) return ienum.GetGenericArguments()[0];
        
        return type;
    }

    /// <summary>
    /// Returns a cached, dialect-specific INSERT template for the given entity/DTO pair.
    /// </summary>
    /// <remarks>
    /// Built by running a canonical INSERT segment stream through the dialect's rewriter pipeline.
    /// The format string is split at <c>VALUES</c> and stored in a <see cref="SqlBulkTemplate"/>
    /// to support multi-row inserts in a single statement.
    /// </remarks>
    public static ISqlTemplate GetInsertTemplate<TEntity, TDto>(ISqlDialect dialect)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        return _insertCache.GetOrAdd((typeof(TEntity), dtoType, dialect.Kind), _ =>
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);

            var table = new SqlTable<TEntity>(meta.Name, meta.Schema, alias: null);

            var columns = new List<(SqlRawColumnReference ColRef, Func<object, object?> Extractor)>();
            foreach (var prop in props)
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey == null) continue;
                columns.Add((new SqlRawColumnReference(table.Reference, meta.Columns[matchingKey]), getters[prop.Name]));
            }

            var assignments = columns.Select(c => (ISqlAssignmentFragment)new SqlAssignmentFragment(c.ColRef, null)).ToList();
            var insertFragment = new SqlInsertValuesFragment(assignments);

            // A space (not newline) between entity and fragment preserves the existing
            // output format: "INSERT INTO [table] ([col1], [col2])\nVALUES ..."
            var segments = new List<SqlSegment>
            {
                new(SqlSegmentType.Literal, "INSERT INTO ", null, SqlSegmentTag.InsertKeyword),
                new(SqlSegmentType.Reference, table, null, SqlSegmentTag.InsertTarget),
                new(SqlSegmentType.Literal, " "),
                new(SqlSegmentType.Raw, insertFragment)
            };

            var options = dialect.GetDefaultOptions();
            var formatString = RenderSegmentsToFormatString(segments, dialect, options);

            // SqlInsertValuesFragment.ToSql() always emits "(cols)\nVALUES (row)" using
            // Environment.NewLine. Use the same line ending in the split marker so the
            // prefix is clean regardless of whether we're on Windows (\r\n) or Unix (\n).
            var splitMarker = $"{Environment.NewLine}{SqlKeyword.Values.Value} ";
            var splitIdx = formatString.LastIndexOf(splitMarker, StringComparison.OrdinalIgnoreCase);

            string insertPrefix, rowFormat;
            if (splitIdx >= 0)
            {
                insertPrefix = formatString[..splitIdx];
                rowFormat    = formatString[(splitIdx + splitMarker.Length)..];
            }
            else
            {
                // Defensive: a rewriter completely restructured the INSERT statement.
                insertPrefix = formatString;
                rowFormat    = "(" + string.Join(", ", Enumerable.Range(0, columns.Count).Select(i => $"{{{i}}}")) + ")";
            }

            var extractors = columns.Select(c => c.Extractor).ToList();
            return new SqlBulkTemplate(insertPrefix, rowFormat, item =>
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }

    /// <summary>
    /// Returns a cached, dialect-specific UPDATE template for the given entity/DTO pair and key columns.
    /// </summary>
    public static ISqlTemplate GetUpdateTemplate<TEntity, TDto>(ISqlDialect dialect, string[] keyProperties)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        var cacheKey = (typeof(TEntity), dtoType, dialect.Kind, string.Join("|", keyProperties));

        return _updateCache.GetOrAdd(cacheKey, _ =>
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);
            var keySet = new HashSet<string>(keyProperties, StringComparer.OrdinalIgnoreCase);

            var table = new SqlTable<TEntity>(meta.Name, meta.Schema, alias: null);

            // SET columns: non-key columns in DTO property order
            var setColumns = new List<(SqlRawColumnReference ColRef, Func<object, object?> Extractor)>();
            foreach (var prop in props.Where(p => !keySet.Contains(p.Name)))
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey == null) continue;
                setColumns.Add((new SqlRawColumnReference(table.Reference, meta.Columns[matchingKey]), getters[prop.Name]));
            }

            // WHERE columns: key columns in the caller-specified order
            var whereColumns = new List<(SqlRawColumnReference ColRef, Func<object, object?> Extractor)>();
            foreach (var keyName in keyProperties)
            {
                var prop = props.FirstOrDefault(p => p.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                if (prop == null) continue;
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey == null) continue;
                whereColumns.Add((new SqlRawColumnReference(table.Reference, meta.Columns[matchingKey]), getters[prop.Name]));
            }

            if (setColumns.Count == 0) throw new InvalidOperationException($"No updatable columns found for {dtoType.Name}.");
            if (whereColumns.Count == 0) throw new InvalidOperationException($"No key columns defined for UPDATE on {typeof(TEntity).Name}.");

            var setFragment = new SqlSetFragment(
                setColumns.Select(c => (ISqlAssignmentFragment)new SqlAssignmentFragment(c.ColRef, null)));

            var whereAssignments = whereColumns
                .Select(c => new SqlAssignmentFragment(c.ColRef, null)).ToList();

            // Segments: "UPDATE table SET col={0} WHERE col={n}"
            // A space (not newline) before WHERE preserves the existing single-line output format.
            var segments = new List<SqlSegment>
            {
                new(SqlSegmentType.Literal, "UPDATE ", null, SqlSegmentTag.UpdateKeyword),
                new(SqlSegmentType.Reference, table, null, SqlSegmentTag.UpdateTarget),
                new(SqlSegmentType.Literal, " "),
                new(SqlSegmentType.Raw, setFragment),
                new(SqlSegmentType.Literal, " WHERE ", null, SqlSegmentTag.WhereKeyword)
            };

            for (int i = 0; i < whereAssignments.Count; i++)
            {
                if (i > 0) segments.Add(new SqlSegment(SqlSegmentType.Literal, " AND "));
                segments.Add(new SqlSegment(SqlSegmentType.Raw, whereAssignments[i]));
            }

            var options = dialect.GetDefaultOptions();
            var formatString = RenderSegmentsToFormatString(segments, dialect, options);

            // Row extractor: SET values first ({0}…), then WHERE values ({n}…)
            var allExtractors = setColumns.Select(c => c.Extractor)
                                          .Concat(whereColumns.Select(c => c.Extractor))
                                          .ToList();
            return new SqlBatchTemplate(formatString, item =>
            {
                var vals = new object?[allExtractors.Count];
                for (int i = 0; i < allExtractors.Count; i++) vals[i] = allExtractors[i](item!);
                return vals;
            });
        });
    }

    /// <summary>
    /// Returns a cached, dialect-specific DELETE template for the given entity/DTO pair and key columns.
    /// </summary>
    public static ISqlTemplate GetDeleteTemplate<TEntity, TDto>(ISqlDialect dialect, string[] keyProperties)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        var cacheKey = (typeof(TEntity), dtoType, dialect.Kind, string.Join("|", keyProperties));

        return _deleteCache.GetOrAdd(cacheKey, _ =>
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);

            var table = new SqlTable<TEntity>(meta.Name, meta.Schema, alias: null);

            var whereColumns = new List<(SqlRawColumnReference ColRef, Func<object, object?> Extractor)>();
            foreach (var keyName in keyProperties)
            {
                var prop = props.FirstOrDefault(p => p.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                if (prop == null) continue;
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey == null) continue;
                whereColumns.Add((new SqlRawColumnReference(table.Reference, meta.Columns[matchingKey]), getters[prop.Name]));
            }

            if (whereColumns.Count == 0) throw new InvalidOperationException($"No key columns defined for DELETE on {typeof(TEntity).Name}.");

            var whereAssignments = whereColumns.Select(c => new SqlAssignmentFragment(c.ColRef, null)).ToList();

            // A space (not newline) before WHERE preserves the existing single-line output format.
            var segments = new List<SqlSegment>
            {
                new(SqlSegmentType.Literal, "DELETE FROM ", null, SqlSegmentTag.DeleteKeyword),
                new(SqlSegmentType.Reference, table),
                new(SqlSegmentType.Literal, " WHERE ", null, SqlSegmentTag.WhereKeyword)
            };

            for (int i = 0; i < whereAssignments.Count; i++)
            {
                if (i > 0) segments.Add(new SqlSegment(SqlSegmentType.Literal, " AND "));
                segments.Add(new SqlSegment(SqlSegmentType.Raw, whereAssignments[i]));
            }

            var options = dialect.GetDefaultOptions();
            var formatString = RenderSegmentsToFormatString(segments, dialect, options);

            var extractors = whereColumns.Select(c => c.Extractor).ToList();
            return new SqlBatchTemplate(formatString, item =>
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }

    /// <summary>
    /// Returns a cached, dialect-specific upsert template for the given entity/DTO pair and key columns.
    /// </summary>
    /// <remarks>
    /// The template is built once per dialect by running a canonical
    /// <c>INSERT … ON CONFLICT … DO UPDATE SET</c> segment stream through the rewriter pipeline
    /// with a <see cref="SqlTemplateContext"/>.  The pipeline transforms the structure into
    /// MERGE (SQL Server), ON DUPLICATE KEY UPDATE (MySQL), or leaves it unchanged
    /// (PostgreSQL / SQLite).  The resulting format string uses <c>{0}</c>, <c>{1}</c>, …
    /// as parameter holes; these are bound to actual values at render time via
    /// <see cref="SqlBatchTemplate.Render"/>.
    /// </remarks>
    /// <typeparam name="TEntity">The target table model type.</typeparam>
    /// <typeparam name="TDto">The data transfer object type supplying values for the operation.</typeparam>
    /// <param name="dialect">The active SQL dialect.</param>
    /// <param name="keyProperties">The C# property names of the conflict key columns.</param>
    /// <returns>A cached <see cref="ISqlTemplate"/> for the upsert statement.</returns>
    /// <exception cref="SqlDialectException">
    /// Thrown when the dialect does not declare <see cref="SqlFeature.OnConflict"/> support.
    /// </exception>
    public static ISqlTemplate GetUpsertTemplate<TEntity, TDto>(ISqlDialect dialect, string[] keyProperties)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        // Sort key names so the cache key is stable regardless of caller order.
        var cacheKey = (typeof(TEntity), dtoType, dialect.Kind, string.Join("|", keyProperties.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)));

        return _upsertCache.GetOrAdd(cacheKey, _ =>
        {
            if (!dialect.SupportedFeatures.Contains(SqlFeature.OnConflict))
                throw new SqlDialectException(dialect.Kind.ToString(), "UPSERT");

            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);
            var keySet = new HashSet<string>(keyProperties, StringComparer.OrdinalIgnoreCase);

            // Use SqlTable<TEntity> as the entity object so the rewriter can find it.
            var table = new SqlTable<TEntity>(meta.Name, meta.Schema, alias: null);

            // Collect (colRef, propertyName, extractor) for each column that appears in the DTO.
            // Sort alphabetically by property name so the INSERT/source column order is stable
            // and matches the test data convention used throughout this framework.
            var columns = new List<(SqlRawColumnReference ColRef, string PropertyName, Func<object, object?> Extractor)>();
            foreach (var prop in props.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey is null) continue;

                var colRef = new SqlRawColumnReference(table.Reference, meta.Columns[matchingKey]);
                columns.Add((colRef, prop.Name, getters[prop.Name]));
            }

            // INSERT assignments: all columns — template context emits {0}, {1}, … for each.
            var insertAssignments = columns
                .Select(c => (ISqlAssignmentFragment)new SqlAssignmentFragment(c.ColRef, null))
                .ToList();

            // DO UPDATE SET assignments: non-key columns with their own parameter holes.
            // This produces separate {n} slots for the update values (e.g. @p3, @p4),
            // so both INSERT VALUES and UPDATE SET use explicit, independently bound parameters.
            var updateAssignments = columns
                .Where(c => !keySet.Contains(c.PropertyName))
                .Select(c => (ISqlAssignmentFragment)new SqlAssignmentFragment(c.ColRef, null))
                .ToList();

            // Conflict (key) column references — the SQL Server rewriter reads them to build the MERGE ON clause.
            var keyColumnRefs = columns
                .Where(c => keySet.Contains(c.PropertyName))
                .Select(c => (ISqlReference)c.ColRef)
                .ToList();

            if (updateAssignments.Count == 0)
                throw new InvalidOperationException($"UPSERT for {typeof(TEntity).Name} requires at least one non-key column to update.");

            if (keyColumnRefs.Count == 0)
                throw new InvalidOperationException($"No key columns resolved for UPSERT on {typeof(TEntity).Name}. Check the key selector.");

            var insertFragment = new SqlInsertValuesFragment(insertAssignments);
            var setFragment = new SqlSetFragment(updateAssignments);

            // Build the canonical segment stream:
            //   INSERT INTO <table> (<cols>) VALUES (<{n}…>)
            //   ON CONFLICT (<keyCol1>, …)
            //   DO UPDATE SET <col> = EXCLUDED.<col>, …
            var segments = new List<SqlSegment>
            {
                new(SqlSegmentType.Literal, "INSERT INTO ", null, SqlSegmentTag.InsertKeyword),
                new(SqlSegmentType.Reference, table, null, SqlSegmentTag.InsertTarget),
                new(SqlSegmentType.Literal, "\n"),
                new(SqlSegmentType.Raw, insertFragment),
                new(SqlSegmentType.Literal, "\nON CONFLICT (", null, SqlSegmentTag.OnConflictKeyword)
            };

            for (int ki = 0; ki < keyColumnRefs.Count; ki++)
            {
                if (ki > 0) segments.Add(new SqlSegment(SqlSegmentType.Literal, ", "));
                segments.Add(new SqlSegment(SqlSegmentType.Reference, keyColumnRefs[ki]));
            }

            segments.Add(new SqlSegment(SqlSegmentType.Literal, ")\nDO UPDATE "));
            segments.Add(new SqlSegment(SqlSegmentType.Raw, setFragment));

            // Run through the rewriter pipeline with a template context so parameter slots
            // become {n} placeholders and dialect-specific structure rewrites happen.
            // dialect.GetDefaultOptions() provides the dialect's rewriters (SqlServerSyntaxRewriter, etc.).
            var options = dialect.GetDefaultOptions();
            string formatString = RenderSegmentsToFormatString(segments, dialect, options);

            // Row extractor: INSERT values (all cols) + UPDATE values (non-key cols).
            // Both INSERT and UPDATE use their own parameter holes in the format string,
            // so the extractor must supply values for both — the same DTO values appear twice.
            var insertExtractors = columns.Select(c => c.Extractor).ToList();
            var updateExtractors = columns.Where(c => !keySet.Contains(c.PropertyName)).Select(c => c.Extractor).ToList();
            var allExtractors = insertExtractors.Concat(updateExtractors).ToList();

            return new SqlBatchTemplate(formatString, item =>
            {
                var vals = new object?[allExtractors.Count];
                for (int i = 0; i < allExtractors.Count; i++) vals[i] = allExtractors[i](item!);
                return vals;
            });
        });
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Runs a pre-built segment list through the dialect's rewriter pipeline using
    /// a <see cref="SqlTemplateContext"/> that emits <c>{n}</c> placeholders for parameters,
    /// then renders the result to a <c>string.Format</c>-compatible format string.
    /// </summary>
    private static string RenderSegmentsToFormatString(
        List<SqlSegment> segments,
        ISqlDialect dialect,
        SqlInterpolOptions options)
    {
        var templateContext = new SqlTemplateContext(dialect, options);
        var preprocessor = options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlPipeline(preprocessor, options.Rewriters);
        var compiledSegments = pipeline.Process(segments, templateContext);

        var renderer = options.Renderer ?? SqlSegmentRenderer.Instance;
        var sb = new StringBuilder();
        for (int i = 0; i < compiledSegments.Count; i++)
            sb.Append(renderer.Render(templateContext, compiledSegments[i], i, compiledSegments) ?? string.Empty);

        return sb.ToString();
    }
}