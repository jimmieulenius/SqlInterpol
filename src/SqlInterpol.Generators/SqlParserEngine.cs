using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using SqlInterpol.Generators.Parsing;

public class SqlParserEngine
{
    public string TranslateSqlToCSharp(string postgresSql)
    {
        var stream = CharStreams.fromString(postgresSql);
        
        var lexer = new PostgreSQLLexer(stream);
        var tokens = new CommonTokenStream(lexer);
        
        var parser = new PostgreSQLParser(tokens);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new ThrowingErrorListener()); 

        // Start parsing. In the official PostgreSQL grammar, the root rule is usually 'root'
        IParseTree tree = parser.root();

        var visitor = new SqlCodeGenerationVisitor();
        return visitor.Visit(tree);
    }
}

public class ThrowingErrorListener : BaseErrorListener
{
    // Note the added 'TextWriter output' parameter at the beginning
    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        throw new Exception($"line {line}:{charPositionInLine} {msg}");
    }
}