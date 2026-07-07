// namespace SqlInterpol.Parsing;

// public partial class SqlSegmentPreprocessor
// {
//     private static IReadOnlyList<SqlSegment>? ExtractInternalSegments(object obj)
//     {
//         if (obj is ISqlSegmentContainer container) return container.Segments;
//         if (obj is ISqlQuery query) return query.Segments; 
//         if (obj is ISqlEntityBase ent)
//         {
//             if (ent is ISqlSegmentContainer entContainer) return entContainer.Segments;
//             if (ent is ISqlQuery entQuery) return entQuery.Segments;
//             if (ent.Reference is ISqlSegmentContainer refContainer) return refContainer.Segments;
//             if (ent.Reference is ISqlQuery refQuery) return refQuery.Segments;
//         }
//         return null;
//     }

//     private bool ProcessSubquery(SqlSegment segment, int currentIndex, IReadOnlyList<SqlSegment> segments, SqlPreprocessorState state)
//     {
//         bool isSubquery = segment.Value is ISqlQueryFragment || (segment.Value is ISqlEntityBase e && e.Reference is ISqlQueryFragment) || segment.Value is ISqlQuery;                  
//         var innerSegments = isSubquery && segment.Value != null ? ExtractInternalSegments(segment.Value) : null;
        
//         if (innerSegments != null)
//         {
//             bool hasLeftParen = false;
//             for (int idx = state.Refined.Count - 1; idx >= 0; idx--)
//             {
//                 if (state.Refined[idx].Type == SqlSegmentType.Literal && state.Refined[idx].Value is string lText)
//                 {
//                     int checkIdx = lText.Length - 1;
//                     while (checkIdx >= 0 && char.IsWhiteSpace(lText[checkIdx])) checkIdx--;
//                     if (checkIdx >= 0) { if (lText[checkIdx] == '(') hasLeftParen = true; break; }
//                 }
//                 else break;
//             }

//             bool hasRightParen = false;
//             for (int idx = currentIndex + 1; idx < segments.Count; idx++)
//             {
//                 if (segments[idx].Type == SqlSegmentType.Literal && segments[idx].Value is string rText)
//                 {
//                     int checkIdx = 0;
//                     while (checkIdx < rText.Length && char.IsWhiteSpace(rText[checkIdx])) checkIdx++;
//                     if (checkIdx < rText.Length) { if (rText[checkIdx] == ')') hasRightParen = true; break; }
//                 }
//                 else break;
//             }

//             var processedInner = (List<SqlSegment>)Process(innerSegments, state.Context);
//             var entityRef = (segment.Value as ISqlEntityBase)?.Reference;
            
//             bool shouldExclude = (hasLeftParen && hasRightParen);
//             if (segment.Value is ISqlQueryFragment qFrag && qFrag.ExcludeParentheses) shouldExclude = true;
            
//             bool hasInlineAlias = false;
//             for (int n = currentIndex + 1; n < segments.Count; n++)
//             {
//                 if (segments[n].Type == SqlSegmentType.Literal && segments[n].Value is string nText)
//                 {
//                     if (string.IsNullOrWhiteSpace(nText)) continue;
                    
//                     // Centralized high-performance lookahead parsing via unified helper
//                     if (TryParseAlias(nText.AsSpan(), out _))
//                     {
//                         hasInlineAlias = true;
//                     }
//                     break;
//                 }
//             }
            
//             var safeRef = (hasInlineAlias && entityRef != null) ? new AliaslessReference(entityRef) : entityRef;
//             var nestedFrag = new SqlNestedQueryFragment(processedInner, safeRef) { ExcludeParentheses = shouldExclude };
//             state.Refined.Add(new SqlSegment(SqlSegmentType.Reference, nestedFrag, segment.RenderMode, segment.Tags));
//             state.ExpectsAlias = false;

//             if (segment.Value is ISqlEntityBase structuralEntity)
//             {
//                 state.ActiveEntityTarget = structuralEntity;
//                 state.LastAliasableTarget = structuralEntity;
//                 state.LastEntityRef = safeRef; 
//             }
//             else
//             {
//                 state.ActiveEntityTarget = nestedFrag;
//                 state.LastAliasableTarget = nestedFrag;
//                 state.LastEntityRef = safeRef; 
//             }

//             return true;
//         }

//         return false;
//     }
// }