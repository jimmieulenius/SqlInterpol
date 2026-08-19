namespace SqlInterpol.Pipeline;

/// <summary>
/// A pure utility class for SQL lexical operations.
/// </summary>
public static class SqlLexicalHelper
{
    /// <summary>
    /// Safely replaces a keyword in a raw SQL string, ignoring occurrences inside strings and comments.
    /// </summary>
    public static string ReplaceKeyword(string sql, string target, string replacement)
    {
        if (string.IsNullOrEmpty(sql) || sql.IndexOf(target, StringComparison.OrdinalIgnoreCase) == -1)
             return sql;
                     
        var sb = new System.Text.StringBuilder(sql.Length);
        bool inString = false, inLineCmt = false, inBlockCmt = false;
        int targetLen = target.Length;
        
        for (int i = 0; i < sql.Length; i++)
        {
            char c = sql[i];
                         
            if (inString) { if (c == '\'') { if (i + 1 < sql.Length && sql[i + 1] == '\'') { sb.Append("''"); i++; continue; } else inString = false; } sb.Append(c); continue; }
            if (inBlockCmt) { if (c == '*' && i + 1 < sql.Length && sql[i + 1] == '/') { inBlockCmt = false; sb.Append("*/"); i++; continue; } sb.Append(c); continue; }
            if (inLineCmt) { if (c == '\n' || c == '\r') inLineCmt = false; sb.Append(c); continue; }
                         
            if (c == '\'') { inString = true; sb.Append(c); continue; }
            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*') { inBlockCmt = true; sb.Append("/*"); i++; continue; }
            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-') { inLineCmt = true; sb.Append("--"); i++; continue; }
            
            if (char.ToUpperInvariant(c) == char.ToUpperInvariant(target[0]))
            {
                if (i + targetLen <= sql.Length && sql.Substring(i, targetLen).Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    bool wordStart = i == 0 || (!char.IsLetterOrDigit(sql[i - 1]) && sql[i - 1] != '_');
                    bool wordEnd = (i + targetLen == sql.Length) || (!char.IsLetterOrDigit(sql[i + targetLen]) && sql[i + targetLen] != '_');
                                         
                    if (wordStart && wordEnd)
                    {
                        sb.Append(replacement);
                        i += targetLen - 1; 
                        continue;
                    }
                }
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
}