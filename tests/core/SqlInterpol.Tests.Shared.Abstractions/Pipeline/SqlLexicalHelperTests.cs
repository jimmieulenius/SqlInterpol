using SqlInterpol.Pipeline;
using Xunit;

namespace SqlInterpol.Tests.Pipeline;

public class SqlLexicalHelperTests
{
    [Theory]
    // 1. Standard Replacements & Case Insensitivity
    [InlineData("SELECT 1 EXCEPT SELECT 2", "SELECT 1 MINUS SELECT 2")]
    [InlineData("SELECT 1 except SELECT 2", "SELECT 1 MINUS SELECT 2")]
    [InlineData("EXCEPT EXCEPT", "MINUS MINUS")]
    [InlineData("EXCEPT\nEXCEPT", "MINUS\nMINUS")]
    
    // 2. Word Boundaries (Must not replace substrings)
    [InlineData("SELECT UNEXCEPTED", "SELECT UNEXCEPTED")]
    [InlineData("SELECT EXCEPTIONAL", "SELECT EXCEPTIONAL")]
    [InlineData("SELECT MY_EXCEPT_COLUMN", "SELECT MY_EXCEPT_COLUMN")]
    
    // 3. String Escapes (Must ignore keywords inside quotes)
    [InlineData("SELECT 'Everyone EXCEPT you'", "SELECT 'Everyone EXCEPT you'")]
    [InlineData("SELECT 'It''s EXCEPT here'", "SELECT 'It''s EXCEPT here'")]
    [InlineData("EXCEPT 'EXCEPT' EXCEPT", "MINUS 'EXCEPT' MINUS")]
    
    // 4. Line Comments (Must ignore keywords after --)
    [InlineData("SELECT 1 -- EXCEPT this", "SELECT 1 -- EXCEPT this")]
    [InlineData("SELECT 1 -- EXCEPT\n EXCEPT", "SELECT 1 -- EXCEPT\n MINUS")]
    
    // 5. Block Comments (Must ignore keywords inside /* */)
    [InlineData("SELECT 1 /* EXCEPT */", "SELECT 1 /* EXCEPT */")]
    [InlineData("EXCEPT /* EXCEPT */ EXCEPT", "MINUS /* EXCEPT */ MINUS")]
    
    // 6. The Ultimate Mixed Query
    [InlineData(
        "EXCEPT 'EXCEPT' /* EXCEPT */ -- EXCEPT\n EXCEPT", 
        "MINUS 'EXCEPT' /* EXCEPT */ -- EXCEPT\n MINUS")]
    public void ReplaceKeyword_SafelyReplacesTarget(string input, string expected)
    {
        // Act
        var result = SqlLexicalHelper.ReplaceKeyword(input, "EXCEPT", "MINUS");

        // Assert
        Assert.Equal(expected, result);
    }
}