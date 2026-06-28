// using SqlInterpol.Parsing;

// namespace SqlInterpol.Dialects;

// /// <summary>
// /// The Firebird dialect: double-quote identifiers, FIRST/SKIP paging, WITH LOCK row locking,
// /// and MERGE-based multi-table UPDATE and EXISTS-based multi-table DELETE.
// /// </summary>
// public class FirebirdSqlDialect : SqlDialectBase
// {
//     public override SqlDialectKind Kind => SqlDialectKind.Firebird;
//     public override string OpenQuote => "\"";
//     public override string CloseQuote => "\"";
//     public override string ParameterPrefix => "@p";
//     public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
//     {
//         SqlFeature.ForUpdate,
//         SqlFeature.Returning,
//     };
//     public override int QueryParametersMaxCount => 1499;

//     /// <inheritdoc />
//     public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
//     {
//         if (fragment is SqlPagingFragment p)
//         {
//             return $"FIRST {p.Limit} SKIP {p.Offset}";
//         }

//         if (fragment is SqlLockFragment)
//         {
//             return string.Empty;
//         }

//         if (fragment is SqlMultiTableUpdateFragment update && update.FromClause != null)
//         {
//             var target = update.Target.ToSql(context).Trim();
//             var setClause = update.SetClause.ToSql(context).Trim();
//             var fromClause = update.FromClause.ToSql(context).Trim();
//             var whereClause = update.WhereClause?.ToSql(context).Trim() ?? "1=1";

//             fromClause = SqlInterpolationParser.Instance.ReplaceKeyword(fromClause, "AS", "").Replace("  ", " ");

//             return $"MERGE INTO {target}{Environment.NewLine}USING {fromClause}{Environment.NewLine}ON ({whereClause}){Environment.NewLine}WHEN MATCHED THEN UPDATE SET {setClause}";
//         }

//         if (fragment is SqlMultiTableDeleteFragment delete)
//         {
//             var targetDecl = delete.Target.ToSql(context).Trim();
//             var fromClause = delete.FromClause.ToSql(context).Trim();
//             var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";

//             fromClause = SqlInterpolationParser.Instance.ReplaceKeyword(fromClause, "AS", "").Replace("  ", " ");
//             var indent = new string(' ', context.Options.IndentSize);

//             return $"DELETE FROM {targetDecl}{Environment.NewLine}WHERE EXISTS ({Environment.NewLine}{indent}SELECT 1{Environment.NewLine}{indent}FROM {fromClause}{Environment.NewLine}{indent}WHERE {whereClause}{Environment.NewLine})";
//         }

//         return base.RenderFragment(fragment, context);
//     }

//     /// <inheritdoc />
//     public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
//     {
//         var baseRewritten = base.RewriteSegments(segments).ToList();
//         var rewritten = new List<SqlSegment>(baseRewritten.Count);

//         SqlLockMode? deferredLock = null;

//         for (int i = 0; i < baseRewritten.Count; i++)
//         {
//             var segment = baseRewritten[i];

//             if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
//             {
//                 deferredLock = lockFrag.Mode;
//                 continue;
//             }

//             if (segment.Tag == SqlSegmentTag.Paging && segment.Value is string pagingValue)
//             {
//                 if (i + 3 < baseRewritten.Count &&
//                     baseRewritten[i + 1].Type == SqlSegmentType.Parameter &&
//                     baseRewritten[i + 3].Type == SqlSegmentType.Parameter)
//                 {
//                     int index = pagingValue.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);

//                     if (index > -1)
//                     {
//                         rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));
//                     }

//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "FIRST "));
//                     rewritten.Add(baseRewritten[i + 1]); // limit param
//                     rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " SKIP "));
//                     rewritten.Add(baseRewritten[i + 3]); // offset param

//                     i += 3;
//                     continue;
//                 }
//             }

//             rewritten.Add(segment);
//         }

//         if (deferredLock == SqlLockMode.Update)
//         {
//             rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));
//         }

//         return rewritten;
//     }
// }
