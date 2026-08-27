using System.Collections.Concurrent;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ITemplateTestSuite))]
public abstract partial class TemplateTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    private static readonly ConcurrentDictionary<SqlDialectKind, ISqlTemplate> _activeOrderTemplates = new();
    private static readonly ConcurrentDictionary<SqlDialectKind, ISqlTemplate> _manualInsertTemplates = new();
    private static readonly ConcurrentDictionary<SqlDialectKind, ISqlTemplate> _manualUpdateTemplates = new();

    private ISqlTemplate CompileOrderTemplate()
    {
        var db = CreateBuilder();
        db.Entity<TemplateOrderModel>(out var o);
        
#pragma warning disable SQLIG10
        db.Template(out var template, $$"""
            SELECT {{o.Id}}, {{o.CustomerId}}
            FROM {{o}} AS o1
            WHERE {{o.CustomerId}} = {{Sql.Arg("CustId")}}
            """);
#pragma warning restore SQLIG10
            
        return template;
    }

    private ISqlTemplate CompileManualInsertTemplate()
    {
        var db = CreateBuilder();
        db.Entity<TemplateOrderModel>(out var o);

#pragma warning disable SQLIG10
        // The macro dynamically expands into (Col1, Col2) VALUES ({0}, {1}) and maps the args natively!
        db.Template(out var template, $$"""
            INSERT INTO {{o}}
            VALUES {{Sql.Expand<OrderInsertPayload>()}}
            """);
#pragma warning restore SQLIG10

        return template;
    }

    private ISqlTemplate CompileManualUpdateTemplate()
    {
        var db = CreateBuilder();
        db.Entity<TemplateOrderModel>(out var o);

#pragma warning disable SQLIG10
        // Passing "Id" excludes it from the SET list, allowing us to map it safely in the WHERE clause!
        db.Template(out var template, $$"""
            UPDATE {{o}}
            SET {{Sql.Expand<OrderUpdatePayload>("Id")}}
            WHERE {{o.Id:col}} = {{Sql.Arg("Id")}}
            """);
#pragma warning restore SQLIG10

        return template;
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateSelectData))]
    public void Template_Select(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();
            
            db.Entity<TemplateOrderModel>(out var o, "o1"); 

            var activeOrderTemplate = _activeOrderTemplates.GetOrAdd(
                db.Context.Dialect.Kind, 
                _ => CompileOrderTemplate());

#pragma warning disable SQLIG10
            return db.Append(activeOrderTemplate, new { CustId = 5 })
                     .AppendLine()
                     .Append($"ORDER BY {o.Id} DESC") 
                     .Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateBulkInsertData))]
    public void Template_BulkInsert(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();
            db.Entity<TemplateOrderModel>(out var o);

            var payloads = new[]
            {
                new OrderIdPayload { Id = 101 },
                new OrderIdPayload { Id = 102 }
            };

            return db.AppendInsert(o, payloads).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateUpdateData))]
    public void Template_Update(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();
            db.Entity<TemplateOrderModel>(out var o);

            var payloads = new[]
            {
                new OrderUpdatePayload { Id = 101, CustomerId = 500 },
                new OrderUpdatePayload { Id = 102, CustomerId = 501 }
            };

            return db.AppendUpdate(o, o.Id, payloads).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateDeleteData))]
    public void Template_Delete(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();
            db.Entity<TemplateOrderModel>(out var o);

            var payloads = new[]
            {
                new OrderDeletePayload { Id = 101 },
                new OrderDeletePayload { Id = 102 }
            };

            return db.AppendDelete(o, o.Id, payloads).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateManualInsertData))]
    public void Template_ManualInsert(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();

            var template = _manualInsertTemplates.GetOrAdd(
                db.Context.Dialect.Kind,
                _ => CompileManualInsertTemplate());

            return db.Append(template, new OrderInsertPayload { Id = 101, CustomerId = 5 }).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ITemplateTestSuite.TemplateManualUpdateData))]
    public void Template_ManualUpdate(SqlTestCase testCase)
    {
        testCase.Act(() =>
        {
            var db = CreateBuilder();

            var template = _manualUpdateTemplates.GetOrAdd(
                db.Context.Dialect.Kind,
                _ => CompileManualUpdateTemplate());

            return db.Append(template, new OrderUpdatePayload { Id = 101, CustomerId = 500 }).Build();
        });

        testCase.Assert();
    }
}