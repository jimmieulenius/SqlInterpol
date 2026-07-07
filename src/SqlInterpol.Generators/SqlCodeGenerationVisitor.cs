using Antlr4.Runtime.Tree;
using SqlInterpol.Generators.Parsing;

public class SqlCodeGenerationVisitor : PostgreSQLParserBaseVisitor<string>
{
    // Override the aggregate method to easily combine child node results
    protected override string AggregateResult(string aggregate, string nextResult)
    {
        if (aggregate == null) return nextResult;
        if (nextResult == null) return aggregate;
        return aggregate + nextResult;
    }

    /// <summary>
    /// EXAMPLE 1: Visiting a String Concatenation expression (||)
    /// </summary>
    public override string VisitA_expr(PostgreSQLParser.A_exprContext context)
    {
        // Check if this is a binary expression specifically using the '||' operator
        if (context.ChildCount == 3 && context.GetChild(1).GetText() == "||")
        {
            string leftSide = Visit(context.GetChild(0));
            string rightSide = Visit(context.GetChild(2));
            
            return $"Sql.Concat({leftSide}, {rightSide})";
        }
        
        return base.VisitA_expr(context);
    }

    /// <summary>
    /// EXAMPLE 2: Visiting a LIMIT clause
    /// The official Postgres grammar defines a 'limit_clause' rule.
    /// </summary>
    public override string VisitLimit_clause(PostgreSQLParser.Limit_clauseContext context)
    {
        // limit_clause is typically: LIMIT select_limit_value | LIMIT ALL
        
        // Check if it has a specific value (e.g., a number or a $1 parameter)
        if (context.select_limit_value() != null)
        {
            // Visit the child node to extract the value string
            string limitValue = Visit(context.select_limit_value()); 
            
            return $".Limit({limitValue})";
        }

        // If it was "LIMIT ALL" (where select_limit_value is null), emit nothing
        return string.Empty;
    }

    /// <summary>
    /// EXAMPLE 3: Visiting a parameter ($1, $2)
    /// In Postgres, parameters are Lexer terminal tokens (PARAM). We catch them here.
    /// </summary>
    public override string VisitTerminal(ITerminalNode node)
    {
        // Check if the current leaf node is a PARAM token
        if (node.Symbol.Type == PostgreSQLParser.PARAM)
        {
            string paramText = node.GetText(); // e.g., "$1"
            
            // Extract the integer index (subtract 1 for zero-based array indexing)
            int paramIndex = int.Parse(paramText.Substring(1)) - 1; 

            // Emit the C# syntax for injecting the runtime parameter
            return $"Sql.Param(args[{paramIndex}])";
        }

        return base.VisitTerminal(node);
    }

    /// <summary>
    /// EXAMPLE 4: Simple column reference
    /// </summary>
    public override string VisitColumnref(PostgreSQLParser.ColumnrefContext context)
    {
        string columnName = context.GetText();
        return $"Sql.Column(\"{columnName}\")";
    }
}