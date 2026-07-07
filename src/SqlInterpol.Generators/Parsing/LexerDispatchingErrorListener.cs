using Antlr4.Runtime;

namespace SqlInterpol.Generators.Parsing;

public class LexerDispatchingErrorListener : IAntlrErrorListener<int>
{
    private Lexer _parent;

    public LexerDispatchingErrorListener(Lexer parent)
    {
        _parent = parent;
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var listener = _parent?.ErrorListeners.FirstOrDefault();
        if (listener != null)
        {
            listener.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
        }
    }
}