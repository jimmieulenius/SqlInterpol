using System;
using System.Collections.Generic;
using System.Reflection;
using SqlInterpol.Configuration;
using SqlInterpol.Execution;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IUpdateTestSuite))]
public abstract partial class UpdateTestSuite
{
    // Shared test data at the class level ensures zero drift between execution and assertions!
    protected const int TargetOrderId = 42;
    protected const string TargetStatus = "Shipped";
    protected const decimal TargetTotal = 99.99m;
    protected static readonly TestUser TemplateUser = new() { Id = 1, Name = "Bob", Age = 31 };

    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IUpdateTestSuite.UpdateData))]
    public void Update_WithContextualDto(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var updateDto = new { Status = TargetStatus, Total = TargetTotal };

        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);

            return db.Append($$"""
                UPDATE {{o}}
                SET {{updateDto}}
                WHERE {{o.Id}} = {{TargetOrderId}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpdateTestSuite.UpdateExplicitData))]
    public void Update_PureManual(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
                UPDATE {{o}}
                SET {{o.Status}} = {{TargetStatus}}, {{o.Total}} = {{TargetTotal}}
                WHERE {{o.Id}} = {{TargetOrderId}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpdateTestSuite.UpdateWithIgnoreData))]
    public void Update_WithIgnoredProperty(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var order = new OrderWithIgnoreModel 
        { 
            Id = TargetOrderId, 
            Status = TargetStatus, 
            Total = TargetTotal, 
            InternalNotes = "Ignore me!" 
        };

        testCase.Act(() => 
        {
            db.Entity<OrderWithIgnoreModel>(out var o);

#pragma warning disable SQLIG10
            return db.Append($$"""
                UPDATE {{o}}
                SET {{order}}
                WHERE {{o.Id}} = {{order.Id}}
                """).Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpdateTestSuite.UpdateInvalidEntityData))]
    public void Update_InvalidEntity(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() =>
        {
            // By using a DispatchProxy, we generate an ISqlEntityBase instance on the fly!
            // Because it is NOT an ISqlEntityBase<T>, it perfectly triggers the runtime ArgumentException!
            var dummyEntity = DispatchProxy.Create<ISqlEntityBase, DummyEntityProxy>();
            
            Sql.BuildAssignments(dummyEntity, new { Name = "Test" }, db.Context);
            return new SqlQueryResult(string.Empty, new Dictionary<string, object?>());
        });

        testCase.Assert();
    }

    // Proactively added this missing test method to cover the TheoryData you provided!
    [SqlTest(nameof(IUpdateTestSuite.UpdateInvalidEntityPropertyData))]
    public void Update_InvalidEntityProperty(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);

            // Passing a DTO with a property that doesn't exist on the entity 
            // will internally trigger Sql.BuildAssignments and throw the expected Exception!
            return db.Append($$"""
                UPDATE {{o}}
                SET {{new { NonExistentProperty = "Test" }}}
                WHERE {{o.Id}} = 1
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpdateTestSuite.MultiTableUpdateData))]
    public void Update_MultiTable(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        // Act - Implicit Join WYSIWYG!
        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<Product>(out var p)
              .Entity<Category>(out var c);

            return db.Append($$"""
                UPDATE {{p}}
                SET {{p.Price}} = {{10}}
                FROM {{c}} AS c1
                WHERE {{p.CategoryId}} = c1.Id
                """).Build();
            #pragma warning restore SQLIG10
        });

        testCase.Assert();
    }
}