using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using SqlInterpol.Constants;
using SqlInterpol.Models;

namespace SqlInterpol.Handlers;

[InterpolatedStringHandler]
public ref struct SqlQueryInterpolatedStringHandler(int literalLength)
{
    // Precompiled regex patterns for extreme performance (avoid re-compilation per query)
    private static readonly Regex _asPatternRegex = new(@"__OBJ(\d+)__\s+AS\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _asAliasPatternRegex = new(@"\s+AS\s+(__OBJ\d+(?:_[A-Z_]+)?__)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _clauseSpecificPatternRegex = new(@"__OBJ(\d+)_([A-Z_]+?)__", RegexOptions.Compiled);
    // Detects "AS identifier" at the start of a literal following an interpolated object.
    // Used to support the natural SQL syntax: {table} AS alias or {subquery} AS alias
    // Matches unquoted identifiers (AS alias) and quoted identifiers (AS [alias], AS "alias", AS `alias`)
    // Avoids false positives on CAST({col} AS INT) because SqlColumn objects are intentionally excluded from this path.
    // Negative lookahead (?!\s*\)) excludes CAST({col} AS INT) — if the identifier is
    // immediately followed by ')' it's a type keyword, not a column/table alias.
    // Atomic group (?> ) prevents backtracking on the identifier so BIGINT) doesn't
    // partially match as BIGIN (where T is not ')') and slip through the lookahead.
    private static readonly Regex _autoAliasLiteralRegex = new(@"^AS\s+[\[\`""]?((?>[A-Za-z_][A-Za-z0-9_]*))[\]\`""]?(?!\s*\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Post-processing: find bare (unquoted) identifiers after AS that weren't auto-aliased.
    // Skips identifiers already quoted ([, ", `) and those followed by ( or ) which are SQL
    // type keywords (CAST(x AS INT), VARCHAR(100), etc.).
    // Atomic group (?> ) prevents backtracking so BIGINT) doesn't partially match as BIGIN.
    private static readonly Regex _unquotedAsAliasRegex = new(@"\bAS\s+((?>[A-Za-z_][A-Za-z0-9_]*))(?!\s*[\)\(])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Dictionary<int, object?> _objects = [];
    private StringBuilder _builder = new(literalLength);
    private int _objectCount = 0;
    private int _lastObjectIndex = -1;  // Track the last appended object index
    private readonly HashSet<int> _objectsInAsContext = [];  // Track which object INDICES are in " AS " context (per-occurrence, not per-object)

#pragma warning restore CS9113

    public void AppendLiteral(string value)
    {
        if (_lastObjectIndex >= 0)
        {
            var trimmedStart = value.TrimStart();
            var autoAliasMatch = _autoAliasLiteralRegex.Match(trimmedStart);

            if (autoAliasMatch.Success && _objects.TryGetValue(_lastObjectIndex, out var obj))
            {
                var alias = autoAliasMatch.Groups[1].Value;
                bool handled = false;

                if (obj is SqlColumn sqlColumn)
                {
                    // {col} AS alias  →  mutate column alias in place (same as .As("alias"))
                    // Safe because CAST({col} AS INT) is excluded by the negative lookahead in the regex
                    sqlColumn.As(alias);
                    handled = true;
                }
                else if (obj is SqlTable sqlTable)
                {
                    // {table} AS alias  →  call table.As("alias"), strip "AS alias" from literal
                    sqlTable.As(alias);
                    handled = true;
                }
                else if (obj is SqlQuery sqlQuery)
                {
                    // {subquery} AS alias  →  wrap in SqlSubqueryTable (sets _registeredAlias too)
                    _objects[_lastObjectIndex] = sqlQuery.As(alias);
                    handled = true;
                }

                if (handled)
                {
                    // Strip the leading whitespace + "AS identifier" from the literal so the object's
                    // own ToString() renders the alias — avoids double-output like [Table] AS [t] AS t
                    int leadingWsLen = value.Length - trimmedStart.Length;
                    value = value.Substring(leadingWsLen + autoAliasMatch.Length);
                    _builder.Append(value);
                    
                    return;
                }
            }

            // Existing behaviour: mark object as in AS context for {table} AS {table.Alias("t")} style
            if (trimmedStart.StartsWith(SqlKeyword.As, StringComparison.OrdinalIgnoreCase))
            {
                // Track by INDEX so the same object can appear multiple times with different AS contexts
                _objectsInAsContext.Add(_lastObjectIndex);
            }
        }

        _builder.Append(value);
    }

    public void AppendFormatted(object? value)
    {
        // String values need context-aware handling:
        // - If preceded by " AS ", it's definitely an alias/identifier (not a parameter)
        // - Otherwise, regular strings are parameters (should be SQL-injected-safe)
        if (value is string stringValue)
        {
            var builderContent = _builder.ToString();
            bool isPrecededByAs = builderContent.TrimEnd().EndsWith($" {SqlKeyword.As}", StringComparison.OrdinalIgnoreCase);
            
            // Only if preceded by AS is it a literal identifier
            // Don't check stringValue content - "[TEST]" is a valid parameter value
            if (isPrecededByAs)
            {
                _builder.Append(stringValue);
                
                return;
            }
            
            // Regular string value - treat as parameter (fall through to normal object handling)
        }
        
        // Store the object itself, NOT its string representation yet
        _objects[_objectCount] = value;
        _builder.Append($"__OBJ{_objectCount}__");
        _lastObjectIndex = _objectCount;  // Track this as the last appended object
        _objectCount++;
    }

    public SqlQuery ToQuery()
    {
        return ToQuery(Sql.CurrentOptions);
    }

    public SqlQuery ToQuery(SqlQueryOptions options)
    {
        string result = _builder.ToString();
        // Preprocess to associate comments with the correct clause
        result = AttachCommentsToClauses(result);
        // Reorder clauses to canonical SQL order, preserving comments
        result = ReorderClausesWithComments(result);
        var parameters = new Dictionary<string, object?>();
        var paramCount = 0;

        // Define SQL clause evaluation order
        var clauseOrder = new[] { SqlKeyword.Insert, SqlKeyword.Update, SqlKeyword.Delete, SqlKeyword.Set, SqlKeyword.Select, SqlKeyword.From, SqlKeyword.On, SqlKeyword.Where, SqlKeyword.GroupBy, SqlKeyword.Having, SqlKeyword.OrderBy, SqlKeyword.Values };
        var processedPlaceholders = new HashSet<string>();  // Track clause-specific placeholders
        
        // PHASE 1: Replace generic placeholders with clause-specific ones
        foreach (var clause in clauseOrder)
        {
            var clausePattern = new Regex($@"{clause}\b([\s\S]*?)(?={SqlKeyword.Insert}|{SqlKeyword.Update}|{SqlKeyword.Delete}|{SqlKeyword.Set}|{SqlKeyword.From}|{SqlKeyword.On}|{SqlKeyword.Where}|{Regex.Escape(SqlKeyword.GroupBy)}|{SqlKeyword.Having}|{Regex.Escape(SqlKeyword.OrderBy)}|{SqlKeyword.Select}|{SqlKeyword.Values}|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            
            foreach (Match clauseMatch in clausePattern.Matches(result))
            {
                var clauseContent = clauseMatch.Value;
                var placeholderMatches = Regex.Matches(clauseContent, @"__OBJ(\d+)__");
                var newClauseContent = clauseContent;
                
                foreach (Match placeholderMatch in placeholderMatches)
                {
                    if (int.TryParse(placeholderMatch.Groups[1].Value, out int index))
                    {
                        var genericPlaceholder = $"__OBJ{index}__";
                        // Replace spaces with underscores in clause name for regex matching
                        var normalizedClause = clause.Replace(" ", "_");
                        var clauseSpecificPlaceholder = $"__OBJ{index}_{normalizedClause}__";
                        
                        newClauseContent = newClauseContent.Replace(genericPlaceholder, clauseSpecificPlaceholder, StringComparison.Ordinal);
                    }
                }
                
                if (newClauseContent != clauseContent)
                {
                    result = result.Replace(clauseContent, newClauseContent);
                }
            }
        }
        
        // PHASE 2: Process clause-specific placeholders and extract parameters
        foreach (Match match in _clauseSpecificPatternRegex.Matches(result))
        {
            if (int.TryParse(match.Groups[1].Value, out int index) && _objects.TryGetValue(index, out var obj))
            {
                var clauseNormalized = match.Groups[2].Value;  // Already has underscores
                // Convert back to original clause name for ToString()
                var clause = clauseNormalized.Replace("_", " ");
                var placeholder = match.Value;
                
                if (!processedPlaceholders.Contains(placeholder))
                {
                    string stringValue;
                    
                    if (obj is SqlTableJoin join)
                    {
                        // Handle JOIN with embedded parameters from AdditionalConditions
                        stringValue = join.ToString(clause, options);
                        
                        // Collect all parameters from all conditions in order, tracking their positions
                        var replacements = new List<(int searchStart, string oldName, string newName, int length)>();
                        int currentSearchStart = 0;
                        
                        foreach (var condition in join.GetAdditionalConditions())
                        {
                            foreach (var (paramName, paramValue) in condition.EmbeddedParameters)
                            {
                                // Create a new parameter name for EVERY occurrence
                                string newParamName = options.UsePositionalParameters 
                                    ? $"{options.ParameterPrefix}{paramCount + 1}"
                                    : $"{options.ParameterPrefix}p{paramCount}";
                                
                                // Find the next occurrence of this parameter name starting from where we left off
                                int foundIndex = stringValue.IndexOf(paramName, currentSearchStart);

                                if (foundIndex >= 0)
                                {
                                    // Verify it's a whole word (not part of a larger identifier)
                                    bool isWordBoundaryBefore = (foundIndex == 0 || !char.IsLetterOrDigit(stringValue[foundIndex - 1]));
                                    bool isWordBoundaryAfter = (foundIndex + paramName.Length >= stringValue.Length || 
                                        !char.IsLetterOrDigit(stringValue[foundIndex + paramName.Length]));
                                    
                                    if (isWordBoundaryBefore && isWordBoundaryAfter)
                                    {
                                        replacements.Add((foundIndex, paramName, newParamName, paramName.Length));
                                        currentSearchStart = foundIndex + paramName.Length;
                                    }
                                }
                                
                                parameters[newParamName] = paramValue;
                                paramCount++;
                            }
                        }
                        
                        // Apply all replacements from right to left to avoid index shifting
                        for (int i = replacements.Count - 1; i >= 0; i--)
                        {
                            var (searchStart, oldName, newName, length) = replacements[i];
                            stringValue = stringValue.Substring(0, searchStart) + newName + stringValue.Substring(searchStart + length);
                        }
                    }
                    else if (obj is SqlReference sqlRef)
                    {
                        // Pass context to ToString - it manages IsAsAlias internally with try/finally cleanup
                        bool isInAsContext = _objectsInAsContext.Contains(index);
                        stringValue = sqlRef.ToString(clause, options, isInAsContext);
                    }
                    else if (obj is SqlSubqueryTable subqueryTable)
                    {
                        // Handle subqueries with aliases: (SELECT ...) AS alias with column access
                        // Re-quote identifiers from the subquery's own dialect to the outer query's dialect,
                        // then quote the alias using the outer options.
                        var innerSql = ReQuoteIdentifiers(subqueryTable.Query.Sql.Trim(), subqueryTable.Query.Options, options);
                        // Format with indented body:
                        //   (
                        //     SELECT ...
                        //     FROM ...
                        //   ) AS [alias]
                        var sqIndent = new string(' ', options.IndentSize);
                        var indentedInner = string.Join("\n", innerSql
                            .Replace("\r\n", "\n").Replace("\r", "\n")
                            .Split('\n')
                            .Select(l => l.Length > 0 ? sqIndent + l : l));
                        stringValue = $"(\n{indentedInner}\n) AS {options.IdentifierStart}{subqueryTable.Alias}{options.IdentifierEnd}";
                        
                        // Renumber all subquery parameters to avoid collisions with main query
                        foreach (var (subParamName, subParamValue) in subqueryTable.EmbeddedParameters)
                        {
                            string newParamName = options.UsePositionalParameters 
                                ? $"{options.ParameterPrefix}{paramCount + 1}"
                                : $"{options.ParameterPrefix}p{paramCount}";
                            
                            // Replace old parameter name with new one in the SQL
                            stringValue = stringValue.Replace(subParamName, newParamName);
                            parameters[newParamName] = subParamValue;
                            paramCount++;
                        }
                    }
                    else if (obj is SqlQuery subquery)
                    {
                        // Handle subqueries without aliases: (SELECT ...) - wrap in parentheses
                        // Re-quote identifiers from the subquery's own dialect to the outer query's dialect.
                        var innerSql = ReQuoteIdentifiers(subquery.Sql.Trim(), subquery.Options, options);
                        stringValue = $"({innerSql})";
                        
                        // Renumber all subquery parameters to avoid collisions with main query
                        foreach (var (subParamName, subParamValue) in subquery.Parameters)
                        {
                            string newParamName = options.UsePositionalParameters 
                                ? $"{options.ParameterPrefix}{paramCount + 1}"
                                : $"{options.ParameterPrefix}p{paramCount}";
                            
                            // Replace old parameter name with new one in the subquery SQL
                            stringValue = stringValue.Replace(subParamName, newParamName);
                            parameters[newParamName] = subParamValue;
                            paramCount++;
                        }
                    }
                    else if (obj is SqlSubqueryColumn subCol)
                    {
                        // Handle subquery column references: [alias].[columnName]
                        stringValue = subCol.ToString(clause, options);
                    }
                    else if (obj is SqlFormat sqlFormat)
                    {
                        // Re-render with detected clause if we have template/args stored
                        if (sqlFormat.Template != null && sqlFormat.Args != null)
                        {
                            stringValue = sqlFormat.Template;
                            for (int i = 0; i < sqlFormat.Args.Length; i++)
                            {
                                var arg = sqlFormat.Args[i];
                                string replacement;

                                if (arg is SqlColumn sqlCol)
                                {
                                    replacement = sqlCol.ToString(clause, options);
                                }
                                else if (arg is SqlReference sqlRefArg)
                                {
                                    replacement = sqlRefArg.Reference;
                                }
                                else if (arg is Sql && !(arg is SqlReference))
                                {
                                    replacement = arg.ToString() ?? string.Empty;
                                }
                                else if (arg == null || (arg is string str && string.IsNullOrEmpty(str)))
                                {
                                    replacement = "";
                                }
                                else if (arg is DBNull)
                                {
                                    replacement = "NULL";
                                }
                                else
                                {
                                    // Literal value - create a parameter for it (it's not already in EmbeddedParameters)
                                    string newParamName = options.UsePositionalParameters 
                                        ? $"{options.ParameterPrefix}{paramCount + 1}"
                                        : $"{options.ParameterPrefix}p{paramCount}";
                                    
                                    parameters[newParamName] = arg;
                                    replacement = newParamName;
                                    paramCount++;
                                }

                                stringValue = stringValue.Replace($"{{{i}}}", replacement);
                            }
                        }
                        else
                        {
                            stringValue = sqlFormat.Value;
                        }
                    }
                    else if (obj is Sql sql && !(obj is SqlReference))
                    {
                        stringValue = sql.Value;
                        
                        // Merge embedded parameters from Sql.Condition() or Sql.Format()
                        // Renumber them to avoid collisions with main query parameters
                        foreach (var (paramName, paramValue) in sql.EmbeddedParameters)
                        {
                            // Create a new parameter name to avoid collisions
                            string newParamName = options.UsePositionalParameters 
                                ? $"{options.ParameterPrefix}{paramCount + 1}"
                                : $"{options.ParameterPrefix}p{paramCount}";
                            
                            // Replace old parameter name with new one in the stringValue
                            stringValue = stringValue.Replace(paramName, newParamName);
                            parameters[newParamName] = paramValue;
                            paramCount++;
                        }
                    }
                    else if (obj is DBNull)
                    {
                        stringValue = "NULL";
                    }
                    else if (obj == null || (obj is string str && string.IsNullOrEmpty(str)))
                    {
                        stringValue = "";
                    }
                    else
                    {
                        // Regular values become parameters (strings, numbers, etc.)
                        string paramName = options.UsePositionalParameters 
                            ? $"{options.ParameterPrefix}{paramCount + 1}"
                            : $"{options.ParameterPrefix}p{paramCount}";
                        parameters[paramName] = obj;
                        stringValue = paramName;
                        paramCount++;
                    }
                    
                    result = result.Replace(placeholder, stringValue);
                    processedPlaceholders.Add(placeholder);
                }
            }
        }
        
        // PHASE 3: Handle any remaining unprocessed generic placeholders
        foreach (var (index, obj) in _objects)
        {
            var pattern = $@"__OBJ{index}__";

            if (Regex.IsMatch(result, pattern))
            {
                string stringValue;
                
                if (obj is SqlTableJoin join)
                {
                    // Handle JOIN with embedded parameters from AdditionalConditions
                    stringValue = join.ToString(SqlKeyword.From, options);
                    
                    // Collect all parameters from all conditions in order, tracking their positions
                    var replacements = new List<(int searchStart, string oldName, string newName, int length)>();
                    int currentSearchStart = 0;
                    
                    foreach (var condition in join.GetAdditionalConditions())
                    {
                        foreach (var (paramName, paramValue) in condition.EmbeddedParameters)
                        {
                            // Create a new parameter name for EVERY occurrence
                            string newParamName = options.UsePositionalParameters 
                                ? $"{options.ParameterPrefix}{paramCount + 1}"
                                : $"{options.ParameterPrefix}p{paramCount}";
                            
                            // Find the next occurrence of this parameter name starting from where we left off
                            int foundIndex = stringValue.IndexOf(paramName, currentSearchStart);

                            if (foundIndex >= 0)
                            {
                                // Verify it's a whole word (not part of a larger identifier)
                                bool isWordBoundaryBefore = (foundIndex == 0 || !char.IsLetterOrDigit(stringValue[foundIndex - 1]));
                                bool isWordBoundaryAfter = (foundIndex + paramName.Length >= stringValue.Length || 
                                    !char.IsLetterOrDigit(stringValue[foundIndex + paramName.Length]));
                                
                                if (isWordBoundaryBefore && isWordBoundaryAfter)
                                {
                                    replacements.Add((foundIndex, paramName, newParamName, paramName.Length));
                                    currentSearchStart = foundIndex + paramName.Length;
                                }
                            }
                            
                            parameters[newParamName] = paramValue;
                            paramCount++;
                        }
                    }
                    
                    // Apply all replacements from right to left to avoid index shifting
                    for (int i = replacements.Count - 1; i >= 0; i--)
                    {
                        var (searchStart, oldName, newName, length) = replacements[i];
                        stringValue = stringValue.Substring(0, searchStart) + newName + stringValue.Substring(searchStart + length);
                    }
                }
                else if (obj is SqlReference sqlRef)
                {
                    // Phase 3 is generic (no clause detected), pass false for isInAsContext
                    stringValue = sqlRef.ToString("DEFAULT", options, isInAsContext: false);
                }
                else if (obj is Sql sql && !(obj is SqlReference))
                {
                    // For SqlFormat objects in generic placeholders, use stored clause
                    stringValue = sql.Value;
                    
                    // Collect all parameters in order, tracking their positions
                    var replacements = new List<(int searchStart, string oldName, string newName, int length)>();
                    int currentSearchStart = 0;
                    
                    // Extract all embedded parameters
                    foreach (var (paramName, paramValue) in sql.EmbeddedParameters)
                    {
                        // Create a new parameter name for EVERY occurrence
                        string newParamName = options.UsePositionalParameters 
                            ? $"{options.ParameterPrefix}{paramCount + 1}"
                            : $"{options.ParameterPrefix}p{paramCount}";
                        
                        // Find the next occurrence of this parameter name starting from where we left off
                        int foundIndex = stringValue.IndexOf(paramName, currentSearchStart);

                        if (foundIndex >= 0)
                        {
                            // Verify it's a whole word (not part of a larger identifier)
                            bool isWordBoundaryBefore = (foundIndex == 0 || !char.IsLetterOrDigit(stringValue[foundIndex - 1]));
                            bool isWordBoundaryAfter = (foundIndex + paramName.Length >= stringValue.Length || 
                                !char.IsLetterOrDigit(stringValue[foundIndex + paramName.Length]));
                            
                            if (isWordBoundaryBefore && isWordBoundaryAfter)
                            {
                                replacements.Add((foundIndex, paramName, newParamName, paramName.Length));
                                currentSearchStart = foundIndex + paramName.Length;
                            }
                        }
                        
                        parameters[newParamName] = paramValue;
                        paramCount++;
                    }
                    
                    // Apply all replacements from right to left to avoid index shifting
                    for (int i = replacements.Count - 1; i >= 0; i--)
                    {
                        var (searchStart, oldName, newName, length) = replacements[i];
                        stringValue = stringValue.Substring(0, searchStart) + newName + stringValue.Substring(searchStart + length);
                    }
                }
                else if (obj is DBNull)
                {
                    stringValue = SqlKeyword.Null;
                }
                else if (obj == null || (obj is string str && string.IsNullOrEmpty(str)))
                {
                    stringValue = "";
                }
                else
                {
                    string paramName = options.UsePositionalParameters 
                        ? $"{options.ParameterPrefix}{paramCount + 1}"
                        : $"{options.ParameterPrefix}p{paramCount}";
                    parameters[paramName] = obj;
                    stringValue = paramName;
                    paramCount++;
                }
                
                result = Regex.Replace(result, pattern, stringValue);
            }
        }

        // PHASE 4: Quote any remaining bare (unquoted) AS aliases in the final SQL.
        // Handles cases like CAST({col} AS BIGINT) AS ConvertedSKU where the outer alias
        // is a plain literal not adjacent to an interpolated object.
        // Already-quoted identifiers ([alias], "alias", `alias`) are left untouched because
        // they don't start with [A-Za-z_].
        var qs = options.IdentifierStart;
        var qe = options.IdentifierEnd;
        result = _unquotedAsAliasRegex.Replace(result, m =>
        {
            var id = m.Groups[1].Value;
            var prefix = m.Value[..(m.Groups[1].Index - m.Index)];  // "AS " portion

            return $"{prefix}{qs}{id}{qe}";
        });

        return new SqlQuery(result, parameters, options);
    }

    private static string ReQuoteIdentifiers(string sql, SqlQueryOptions source, SqlQueryOptions target)
    {
        if (source.IdentifierStart == target.IdentifierStart &&
            source.IdentifierEnd   == target.IdentifierEnd)
        {
            return sql;
        }

        var escapedStart = Regex.Escape(source.IdentifierStart);
        // Build a character-class negation: match everything that isn't the closing delimiter
        var escapedEndForClass = source.IdentifierEnd == "]" ? @"\]" : Regex.Escape(source.IdentifierEnd);
        var pattern = $@"{escapedStart}([^{escapedEndForClass}]+){Regex.Escape(source.IdentifierEnd)}";

        return Regex.Replace(sql, pattern, m =>
            $"{target.IdentifierStart}{m.Groups[1].Value}{target.IdentifierEnd}");
    }

    private static string AttachCommentsToClauses(string sql)
    {
        // SQL comment syntax (-- and /* */) is identical across all supported database dialects:
        // SQL Server, MySQL, PostgreSQL, SQLite, Oracle
        // This implementation is dialect-agnostic and works uniformly for all DatabaseType options
        var clauseKeywords = new[] { SqlKeyword.Insert, SqlKeyword.Update, SqlKeyword.Delete, SqlKeyword.Set, SqlKeyword.Select, SqlKeyword.From, SqlKeyword.On, SqlKeyword.Where, SqlKeyword.GroupBy, SqlKeyword.Having, SqlKeyword.OrderBy, SqlKeyword.Values };
        var lines = sql.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var output = new List<string>();
        var commentBuffer = new List<string>();
        bool inBlockComment = false;
        string? currentClause = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine;
            string trimmed = line.TrimStart();

            // If we're inside a clause, output comments immediately (they're inline to the clause)
            if (currentClause != null)
            {
                output.Add(line);
                // Track block comment state for proper multi-line handling
                if (trimmed.StartsWith("/*"))
                {
                    inBlockComment = true;
                }

                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }

                continue;
            }

            // Before any clause keyword: buffer comments
            if (inBlockComment || trimmed.StartsWith("/*"))
            {
                commentBuffer.Add(line);
                
                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }
                else
                {
                    inBlockComment = true;
                }

                continue;
            }

            // Single-line comment (before any clause)
            if (trimmed.StartsWith("--"))
            {
                commentBuffer.Add(line);

                continue;
            }

            // Clause keyword detection
            var foundClause = clauseKeywords.FirstOrDefault(kw => trimmed.StartsWith(kw, StringComparison.OrdinalIgnoreCase));
            
            if (foundClause != null)
            {
                // Output buffered comments that come before this clause
                if (commentBuffer.Count > 0)
                {
                    output.AddRange(commentBuffer);
                    commentBuffer.Clear();
                }

                currentClause = foundClause;
                output.Add(line);
            }
            else
            {
                // Non-comment, non-clause line before any clause
                output.Add(line);
            }
        }

        // If any comments remain at the end, append them
        if (commentBuffer.Count > 0)
        {
            output.AddRange(commentBuffer);
        }

        return string.Join("\n", output);
    }

    private static string ReorderClausesWithComments(string sql)
    {
        // Reorder SQL clauses (with comments) into canonical SQL order
        // Supports all DatabaseType dialects: SQL Server, MySQL, PostgreSQL, SQLite, Oracle
        // SQL clause ordering is standardized across all major SQL dialects
        var clauseOrder = new[] { SqlKeyword.Insert, SqlKeyword.Update, SqlKeyword.Delete, SqlKeyword.Set, SqlKeyword.Select, SqlKeyword.From, SqlKeyword.On, SqlKeyword.Where, SqlKeyword.GroupBy, SqlKeyword.Having, SqlKeyword.OrderBy, SqlKeyword.Values };
        var lines = sql.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var clauseBlocks = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var trailingLines = new List<string>();
        string? currentClause = null;

        // Initialize all clauses
        foreach (var clause in clauseOrder)
        {
            clauseBlocks[clause] = new List<string>();
        }

        // Group lines by clause: buffer comments before clause, attach all lines until next clause keyword
        var commentBuffer = new List<string>();
        bool inBlockComment = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            // Detect block comment start/end
            if (inBlockComment || trimmed.StartsWith("/*"))
            {
                commentBuffer.Add(line);

                if (trimmed.Contains("*/"))
                {
                    inBlockComment = false;
                }
                else
                {
                    inBlockComment = true;
                }

                continue;
            }

            // Single-line comment (not yet attached to a clause)
            if (trimmed.StartsWith("--"))
            {
                commentBuffer.Add(line);

                continue;
            }

            // Check if this line starts with a clause keyword
            var foundClause = clauseOrder.FirstOrDefault(kw =>
                trimmed.StartsWith(kw, StringComparison.OrdinalIgnoreCase) &&
                (trimmed.Length == kw.Length || !char.IsLetterOrDigit(trimmed[kw.Length])));

            if (foundClause != null)
            {
                // Prepend any buffered comments to this clause
                if (commentBuffer.Count > 0)
                {
                    clauseBlocks[foundClause].AddRange(commentBuffer);
                    commentBuffer.Clear();
                }

                currentClause = foundClause;
                clauseBlocks[foundClause].Add(line);
            }
            else if (currentClause != null)
            {
                // Content line within current clause (non-comment, non-keyword)
                // Prepend any buffered comments that belong to this clause content
                if (commentBuffer.Count > 0 && !trimmed.StartsWith("--") && !trimmed.StartsWith("/*"))
                {
                    clauseBlocks[currentClause].AddRange(commentBuffer);
                    commentBuffer.Clear();
                }

                clauseBlocks[currentClause].Add(line);
            }
            else
            {
                // No current clause and not a keyword -> trailing line
                if (commentBuffer.Count > 0)
                {
                    trailingLines.AddRange(commentBuffer);
                    commentBuffer.Clear();
                }
                
                trailingLines.Add(line);
            }
        }

        // Add any remaining buffered comments to trailing lines
        if (commentBuffer.Count > 0)
        {
            trailingLines.AddRange(commentBuffer);
        }

        // Output in canonical order
        var output = new List<string>();

        foreach (var clause in clauseOrder)
        {
            if (clauseBlocks[clause].Count > 0)
            {
                output.AddRange(clauseBlocks[clause]);
            }
        }

        // Add trailing lines at the end
        if (trailingLines.Count > 0)
        {
            output.AddRange(trailingLines);
        }

        return string.Join("\n", output);
    }
}