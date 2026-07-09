using System.Collections.Generic;
using System.Reflection;
using SqlInterpol.Parsing;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test.Generators;

public class ManualInterceptorSimulationTests
{
    [Fact]
    public void Simulate_Interceptor_For_From_Entity()
    {
        // 1. Arrange
        var db = SqlBuilder.SqlServer(); 
        db.Entity<OrderLine>(out var ol);

        // 2. Simulate the C# Compiler lowering the interpolated string
        var handler = new SqlQueryInterpolatedStringHandler(14, 1, db, out _);
        handler.AppendLiteral("SELECT *\nFROM ");
        
        // The compiler automatically passes "ol" via [CallerArgumentExpression]
        handler.AppendFormatted(ol, format: null, expression: "ol");

        // =========================================================================
        // 3. THE INTERCEPTOR BODY
        // =========================================================================
        
        var genBuilder = (ISqlGeneratorBuilder)db;
        int runtimeIdx = 0;
        
        // The exact AST structural instructions the source generator emits
        genBuilder.AppendRaw("SELECT *\nFROM ");
        
        // =========================================================================
        // FIX: Inline the logic so we don't capture the 'ref struct' handler in a lambda!
        // =========================================================================
        while (runtimeIdx < handler.SegmentCount && handler.GetSegment(runtimeIdx).Type == SqlSegmentType.Literal) 
        {
            runtimeIdx++;
        }
        genBuilder.AppendSegment(handler.GetSegment(runtimeIdx++));

        // =========================================================================
        // 4. Build and Assert
        // =========================================================================
        var result = db.Build();

        // It now correctly outputs the Table with the SQL Server Dialect!
        Assert.Equal("SELECT *\nFROM [OrderLine]", result.Sql);
    }
}