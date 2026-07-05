using SqlInterpol.Parsing;

namespace SqlInterpol.Test.MetaSql.Functions;

/// <summary>
/// A unified, high-performance preprocessor rule that coordinates all custom 
/// Meta-SQL lexical analysis and proprietary syntax translations.
/// </summary>
public partial class MetaSqlPreprocessorRules : ISqlPreprocessorRule
{
    // Custom delegate type required to support high-performance C# 'ref' parameters
    private delegate bool PreprocessorRuleDelegate(
        ref SqlSegment segment, 
        IReadOnlyList<SqlSegment> segments, 
        int index, 
        SqlPreprocessorState state);

    private readonly List<PreprocessorRuleDelegate> _rules = new();

    public MetaSqlPreprocessorRules()
    {
        InitializeRules();
    }

    private void InitializeRules()
    {
        // Centralized location to register your partial rules
        InitializeJsonOperatorRule();
        // InitializeArrayOperatorRule();
    }

    public bool Process(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state)
    {
        // Allocation-free for loop over the rule strategies
        for (int i = 0; i < _rules.Count; i++)
        {
            if (_rules[i](ref segment, segments, index, state))
            {
                return true; // Bypasses core engine processing for this segment
            }
        }
        
        return false; // Allows the segment to fall through to standard core processing
    }

    // Partial method declarations ensuring compilation safety across files
    private partial void InitializeJsonOperatorRule();
}