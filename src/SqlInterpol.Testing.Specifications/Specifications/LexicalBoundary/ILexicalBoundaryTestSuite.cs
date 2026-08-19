using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ILexicalBoundaryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> EscapedQuotesInLiteralData { get; }
    static abstract TheoryData<SqlTestCase> ParameterizedQuotesData { get; }
    static abstract TheoryData<SqlTestCase> MultiLineCommentWithQuotesData { get; }
    static abstract TheoryData<SqlTestCase> SingleLineCommentWithQuotesData { get; }
    static abstract TheoryData<SqlTestCase> StringLiteralWithCommentTokensData { get; }
    static abstract TheoryData<SqlTestCase> KeywordsInLiteralData { get; }
    static abstract TheoryData<SqlTestCase> KeywordsInCommentData { get; }
}