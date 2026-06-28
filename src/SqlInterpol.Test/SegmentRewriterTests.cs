using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;
using SqlInterpol.Parsing;
using SqlInterpol.Test.Models;
using SqlInterpol.Test.Dialects;

namespace SqlInterpol.Test;

public class CustomRewriterTests
{
    [Theory]
    [MemberData(nameof(SoftDeleteData))]
    public void Pipeline_CustomSoftDeleteRewriter(SqlTestCase testCase)
    {
        // Arrange: Create the builder normally, which safely loads the dialect's defaults (like SQLite's index starting at 1)
        var db = testCase.CreateBuilder();
        
        // Inject our custom logic into the active pipeline!
        db.Context.Options.Rewriters.Add(new SoftDeleteRewriter()); 

        int targetId = 42;

        // Act: The user writes a standard DELETE statement
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
                DELETE FROM {{o}}
                WHERE {{o.Id}} = {{targetId}}
                """)
            .Build()
        );

        // Assert: The engine automatically intercepted and rewrote it across ALL dialects!
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> SoftDeleteData
    {
        get
        {
            object?[] expectedParams = [42];

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, [
                    """
                    UPDATE  <<dbo>>.<<Orders>>
                     SET IsDeleted = 1
                    WHERE <<dbo>>.<<Orders>>.<<Id>> = !!100
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.SqlServer, [
                    """
                    UPDATE  [dbo].[Orders]
                     SET IsDeleted = 1
                    WHERE [dbo].[Orders].[Id] = @p0
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.PostgreSql, [
                    """
                    UPDATE  "dbo"."Orders"
                     SET IsDeleted = 1
                    WHERE "dbo"."Orders"."Id" = $1
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.MySql, [
                    """
                    UPDATE  `dbo`.`Orders`
                     SET IsDeleted = 1
                    WHERE `dbo`.`Orders`.`Id` = @p0
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.SqLite, [
                    """
                    UPDATE  "dbo"."Orders"
                     SET IsDeleted = 1
                    WHERE "dbo"."Orders"."Id" = @p1
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.Oracle, [
                    """
                    UPDATE  "dbo"."Orders"
                     SET IsDeleted = 1
                    WHERE "dbo"."Orders"."Id" = :0
                    """], expectedParameters: expectedParams),

                new SqlTestCase(SqlDialectKind.Firebird, [
                    """
                    UPDATE  "dbo"."Orders"
                     SET IsDeleted = 1
                    WHERE "dbo"."Orders"."Id" = @p0
                    """], expectedParameters: expectedParams),
            ];
        }
    }
}

public class SoftDeleteRewriter : ISqlSegmentRewriter
{
    public bool IsApplicable(ISqlCompilationState state) => state.HasTag(SqlSegmentTag.DeleteKeyword);

    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count + 2);
        bool hasInjectedSet = false;

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];

            // 1. Convert DELETE to UPDATE
            if (seg.HasTag(SqlSegmentTag.DeleteKeyword))
            {
                var text = seg.Value?.ToString() ?? "";
                text = Regex.Replace(text, @"\bDELETE\b", "UPDATE", RegexOptions.IgnoreCase);
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text, seg.RenderMode, SqlSegmentTag.UpdateKeyword));
            }
            // 2. Erase the standalone FROM keyword so it becomes "UPDATE Table" instead of "UPDATE FROM Table"
            else if (seg.HasTag(SqlSegmentTag.FromKeyword))
            {
                var text = seg.Value?.ToString() ?? "";
                text = Regex.Replace(text, @"\bFROM\b", "", RegexOptions.IgnoreCase);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // Pass the existing tags array forward
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text, seg.RenderMode, seg.Tags));
                }
            }
            // 3. Inject the SET clause right before the WHERE clause
            else if (seg.HasTag(SqlSegmentTag.WhereKeyword) && !hasInjectedSet)
            {
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $" SET IsDeleted = 1{Environment.NewLine}"));
                rewritten.Add(seg);
                hasInjectedSet = true;
            }
            else
            {
                rewritten.Add(seg);
            }
        }

        return rewritten;
    }
}