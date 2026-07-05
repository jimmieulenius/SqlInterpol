using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class MetaSqlTranspilationTests
{
    // ========================================================================
    // 1. KEYWORD TRANSPILATION (PAGING)
    // ========================================================================
    [Theory]
    [MemberData(nameof(PagingToggleData))]
    public void MetaSql_Paging_Toggle(bool metaSql, SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        
        // Arrange parameters just like a real application
        int limit = 10;
        int offset = 20;
        
        testCase.Action(() => 
        {
            db.Context.Options.MetaSqlTranspilation = metaSql;
            
            // Act: Use string interpolation so the AST can extract the parameter nodes!
            return [db.Append($"SELECT * FROM Products LIMIT {limit} OFFSET {offset}").Build()];
        });

        testCase.Assert();
    }

    public static TheoryData<bool, SqlTestCase> PagingToggleData
    {
        get
        {
            var data = new TheoryData<bool, SqlTestCase>();
            
            // p0 = Limit, p1 = Offset (Note: some dialects use 1-based indexing like Postgres $1, $2)
            object[] expectedParams = [10, 20]; 

            // ====================================================================
            // 1. META-SQL ENABLED (Transpilation ON)
            // ====================================================================
            
            // SQL Server uses OFFSET first, so parameters are swapped: @p1 then @p0
            data.Add(true, new SqlTestCase(SqlDialectKind.SqlServer, 
                ["SELECT * FROM Products OFFSET @p1 ROWS FETCH NEXT @p0 ROWS ONLY"], expectedParams));
            
            // Oracle uses OFFSET first, parameters swapped: :1 then :0
            data.Add(true, new SqlTestCase(SqlDialectKind.Oracle, 
                ["SELECT * FROM Products OFFSET :1 ROWS FETCH NEXT :0 ROWS ONLY"], expectedParams));
            
            data.Add(true, new SqlTestCase(SqlDialectKind.PostgreSql, 
                ["SELECT * FROM Products LIMIT $1 OFFSET $2"], expectedParams));
                
            data.Add(true, new SqlTestCase(SqlDialectKind.MySql, 
                ["SELECT * FROM Products LIMIT @p0 OFFSET @p1"], expectedParams));
                
            data.Add(true, new SqlTestCase(SqlDialectKind.SqLite, 
                ["SELECT * FROM Products LIMIT @p1 OFFSET @p2"], expectedParams));

            // ====================================================================
            // 2. META-SQL DISABLED (Raw Pass-Through)
            // ====================================================================
            // When disabled, the structural rewrite is bypassed. The AST renderer just outputs 
            // the literal "LIMIT ... OFFSET ..." with the correct dialect parameter prefixes!
            
            data.Add(false, new SqlTestCase(SqlDialectKind.SqlServer,  
                ["SELECT * FROM Products LIMIT @p0 OFFSET @p1"], expectedParams));
                
            data.Add(false, new SqlTestCase(SqlDialectKind.Oracle,     
                ["SELECT * FROM Products LIMIT :0 OFFSET :1"], expectedParams));
                
            data.Add(false, new SqlTestCase(SqlDialectKind.PostgreSql, 
                ["SELECT * FROM Products LIMIT $1 OFFSET $2"], expectedParams));
                
            data.Add(false, new SqlTestCase(SqlDialectKind.MySql,      
                ["SELECT * FROM Products LIMIT @p0 OFFSET @p1"], expectedParams));
                
            data.Add(false, new SqlTestCase(SqlDialectKind.SqLite,     
                ["SELECT * FROM Products LIMIT @p1 OFFSET @p2"], expectedParams));

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(PagingHardcodedToggleData))]
    public void MetaSql_Paging_Hardcoded_Toggle(bool metaSql, SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();
        
        testCase.Action(() => 
        {
            db.Context.Options.MetaSqlTranspilation = metaSql;
            
            // Act: Passing a pure, hardcoded string. No interpolation holes!
            return [db.Append($"SELECT * FROM Products LIMIT 10 OFFSET 20").Build()];
        });

        testCase.Assert();
    }

    public static TheoryData<bool, SqlTestCase> PagingHardcodedToggleData
    {
        get
        {
            var data = new TheoryData<bool, SqlTestCase>();
            string rawSql = "SELECT * FROM Products LIMIT 10 OFFSET 20";

            // ====================================================================
            // 1. META-SQL ENABLED (Transpilation ON)
            // ====================================================================
            // Notice there are no expected parameters. The integers are raw literals!
            data.Add(true, new SqlTestCase(SqlDialectKind.SqlServer,  
                ["SELECT * FROM Products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY"]));
                
            data.Add(true, new SqlTestCase(SqlDialectKind.Oracle,     
                ["SELECT * FROM Products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY"]));
                
            data.Add(true, new SqlTestCase(SqlDialectKind.PostgreSql, [rawSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.MySql,      [rawSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.SqLite,     [rawSql]));

            // ====================================================================
            // 2. META-SQL DISABLED (Raw Pass-Through)
            // ====================================================================
            data.Add(false, new SqlTestCase(SqlDialectKind.SqlServer,  [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.Oracle,     [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.PostgreSql, [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.MySql,      [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.SqLite,     [rawSql]));

            return data;
        }
    }

    // ========================================================================
    // 2. EXCEPTION GATEKEEPING (ROW LOCKING)
    // ========================================================================
    [Theory]
    [MemberData(nameof(RowLockingToggleData))]
    public void MetaSql_RowLocking_Toggle(bool metaSql, SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        testCase.Action(() => 
        {
            db.Context.Options.MetaSqlTranspilation = metaSql;
            return [db.Append($"SELECT * FROM Products FOR UPDATE").Build()];
        });

        testCase.Assert();
    }

    public static TheoryData<bool, SqlTestCase> RowLockingToggleData
    {
        get
        {
            var data = new TheoryData<bool, SqlTestCase>();
            string rawSql = "SELECT * FROM Products FOR UPDATE";
            
            // When transpiled, the base rewriter cleanly defers the lock to a new line at the end of the query
            string transpiledSql = "SELECT * FROM Products\nFOR UPDATE";

            // --- META-SQL ENABLED (Transpilation ON) ---
            data.Add(true, new SqlTestCase(SqlDialectKind.SqlServer,  ["SELECT * FROM Products WITH (UPDLOCK)"]));
            data.Add(true, new SqlTestCase(SqlDialectKind.PostgreSql, [transpiledSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.Oracle,     [transpiledSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.MySql,      [transpiledSql]));
            
            // SQLite throws an exception during Builder Validation, which formats it as a list of missing capabilities
            string sqliteError = $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR UPDATE' is not supported by SqLite.";
            data.Add(true, new SqlTestCase(SqlDialectKind.SqLite, typeof(SqlDialectException), sqliteError));

            // --- META-SQL DISABLED (Raw Pass-Through) ---
            // Notice how NO exceptions are thrown when Disabled! Stays completely inline.
            data.Add(false, new SqlTestCase(SqlDialectKind.SqlServer,  [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.PostgreSql, [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.Oracle,     [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.MySql,      [rawSql]));
            
            // SQLite passes it through without throwing. The database will error later, but SqlInterpol succeeds.
            data.Add(false, new SqlTestCase(SqlDialectKind.SqLite,     [rawSql])); 

            return data;
        }
    }

    // ========================================================================
    // 3. AST REWRITER BYPASS (SELECT INTO)
    // ========================================================================
    [Theory]
    [MemberData(nameof(SelectIntoToggleData))]
    public void MetaSql_SelectInto_Toggle(bool metaSql, SqlTestCase testCase)
    {
        var db = testCase.CreateBuilder();

        testCase.Action(() => 
        {
            db.Context.Options.MetaSqlTranspilation = metaSql;
            return [db.Append($"SELECT Id INTO #Temp FROM Products").Build()];
        });

        testCase.Assert();
    }

    public static TheoryData<bool, SqlTestCase> SelectIntoToggleData
    {
        get
        {
            var data = new TheoryData<bool, SqlTestCase>();
            string rawSql = "SELECT Id INTO #Temp FROM Products";

            // --- META-SQL ENABLED (Transpilation ON) ---
            data.Add(true, new SqlTestCase(SqlDialectKind.SqlServer,  [rawSql])); // Native
            data.Add(true, new SqlTestCase(SqlDialectKind.PostgreSql, [rawSql])); // Native
            
            // Rewriter translates this to CREATE TABLE AS SELECT. 
            // Note the preserved trailing space after 'Id ' from the original string literal!
            string createTableSql = "CREATE TABLE \"#Temp\" AS\nSELECT Id \nFROM Products";
            data.Add(true, new SqlTestCase(SqlDialectKind.SqLite, [createTableSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.Oracle, [createTableSql]));
            data.Add(true, new SqlTestCase(SqlDialectKind.MySql,  ["CREATE TABLE `#Temp` AS\nSELECT Id \nFROM Products"]));

            // Firebird throws directly from the Rewriter, so it's a simple error string
            data.Add(true, new SqlTestCase(SqlDialectKind.Firebird, typeof(SqlDialectException), "'SELECT INTO' is not supported"));

            // --- META-SQL DISABLED (Raw Pass-Through) ---
            data.Add(false, new SqlTestCase(SqlDialectKind.SqlServer,  [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.PostgreSql, [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.SqLite,     [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.Oracle,     [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.MySql,      [rawSql]));
            data.Add(false, new SqlTestCase(SqlDialectKind.Firebird,   [rawSql])); // No exception thrown!

            return data;
        }
    }
}