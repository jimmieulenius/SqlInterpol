using SqlInterpol.Test.XvSql.Functions;

namespace SqlInterpol.Test;

/// <summary>
/// This test suite acts as living documentation, proving the three distinct ways 
/// a third-party extension can be loaded into the SqlInterpol engine.
/// </summary>
public class XvSqlExtensionTests
{
    // ====================================================================
    // LOADING PARADIGM 1: Zero-Touch (Standard JIT / Reflection)
    // ====================================================================
    [Fact]
    public void Paradigm1_ZeroTouch_AutoDiscovery()
    {
        // Arrange - The user does absolutely nothing! 
        // The engine's SqlExtensionRegistry automatically scanned the bin folder 
        // or the [ModuleInitializer] fired in the background.
        var options = new SqlInterpolOptions();

        // Act
        var db = SqlBuilder.SqLite(options);
        var sql = db.Append($"SELECT CONCAT_WS(Name, ', ') FROM Users").Build().Sql;

        // Assert - The engine successfully found and applied the extension
        Assert.Contains("group_concat", sql);
        Assert.DoesNotContain("CONCAT_WS", sql);
    }

    // ====================================================================
    // LOADING PARADIGM 2: Instance Explicit (Strict Native AOT & DI)
    // ====================================================================
    [Fact]
    public void Paradigm2_Instance_Explicit_DependencyInjection()
    {
        // Arrange - The user explicitly registers the extension to a specific options instance.
        // This is the standard approach for Strict Native AOT and Microsoft DI setup.
        // e.g. services.AddSqlInterpol(opt => opt.AddExtension(new XvSqlFunctionsExtension()));
        var options = new SqlInterpolOptions()
            .AddExtension(new XvSqlFunctionsExtension());

        // Act
        var db = SqlBuilder.SqLite(options);
        var sql = db.Append($"SELECT CONCAT_WS(Name, ', ') FROM Users").Build().Sql;

        // Assert
        Assert.Contains("group_concat", sql);
    }

    // ====================================================================
    // LOADING PARADIGM 3: Global Explicit (Bootstrappers / Trimming)
    // ====================================================================
    [Fact]
    public void Paradigm3_Global_Explicit_Bootstrapper()
    {
        // Arrange - The developer manually registers the extension globally at App Startup.
        // This prevents the need for Reflection, but ensures every 'new SqlInterpolOptions()' 
        // inherits the extension automatically without explicit DI injection.
        SqlExtensionRegistry.Register(new XvSqlFunctionsExtension());

        var options = new SqlInterpolOptions(); 

        // Act
        var db = SqlBuilder.SqLite(options);
        var sql = db.Append($"SELECT CONCAT_WS(Name, ', ') FROM Users").Build().Sql;

        // Assert
        Assert.Contains("group_concat", sql);
    }

    // ====================================================================
    // VERIFICATION: Proving the Preprocessor Pipeline is active
    // ====================================================================
    [Fact]
    public void Extension_LexicalRule_Successfully_Hijacks_JsonOperator()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act - Using SQL Server to prove the proprietary ->> symbol is intercepted
        var db = SqlBuilder.SqlServer(options);
        var sql = db.Append($"SELECT Data->>'Name' FROM Users").Build().Sql;

        // Assert
        Assert.Contains("Data JSON_EXTRACT 'Name'", sql);
        Assert.DoesNotContain("->>", sql);
    }
    
    [Fact]
    public void Extension_Rewriter_Safely_Passes_Through_Native_Support()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act - PostgreSQL natively supports CONCAT_WS, so the rewriter should ignore it
        var db = SqlBuilder.PostgreSql(options);
        var sql = db.Append($"SELECT CONCAT_WS(Name, ', ') FROM Users").Build().Sql;

        // Assert
        Assert.Contains("CONCAT_WS", sql);
        Assert.DoesNotContain("group_concat", sql);
    }
}