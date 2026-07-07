using Antlr4.Runtime;

namespace SqlInterpol.Generators.Parsing;

public class ParserDispatchingErrorListener : BaseErrorListener
{
    private Parser _parent;

    public ParserDispatchingErrorListener(Parser parent)
    {
        _parent = parent;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var listener = _parent?.ErrorListeners.FirstOrDefault();
        if (listener != null)
        {
            listener.SyntaxError(output, recognizer, offendingSymbol, line, charPositionInLine, msg, e);
        }
    }
}