namespace SqlInterpol.Test.XvSql.Functions;

public class XvSqlFunctionsExtension : ISqlExtension
{
    public void Register(SqlInterpolOptions options)
    {
        // 1. Inject Custom Keywords (Syntax)
        options.KeywordTags["CONCAT_WS"] = ["Meta_ConcatWS"];
        options.KeywordTags["STRING_AGG"] = ["Meta_StringAgg"];
        options.KeywordTags["CAST"] = ["Meta_Cast"];

        // 2. Inject Lexical Rules (Proprietary Operators)
        options.PreprocessorRules.Add(new XvSqlPreprocessorRules());

        // 3. Inject AST Rewriters (Semantics)
        options.Rewriters.Add(new XvSqlFunctionsRewriter());
    }
}