namespace SqlInterpol.Generators;

internal class AotAnalysisResult
    {
        public Dictionary<string, string> InlineAliases { get; } = new();
        public Dictionary<int, string> InlinePropertyAliases { get; } = new();
        public Dictionary<int, string> ReplacementForNextText { get; } = new();

        public bool HasAsKeywordOrAlias { get; set; }
        public bool HasParameterHoles { get; set; }
        public bool HasReturning { get; set; }
        public bool IsDmlQuery { get; set; }
        public bool HasComplexDynamicHoles { get; set; }
        public bool HasSetOperation { get; set; }
        public bool HasUnconsumableAlias { get; set; }
        public bool HasWindowFunction { get; set; }
        public bool HasUpsert { get; set; }
        public bool HasHoleAfterAs { get; set; }
        public string PrePassClause { get; set; } = "UNKNOWN";

        public bool RequiresJitFallback => 
            (HasAsKeywordOrAlias && HasParameterHoles) || 
            HasHoleAfterAs || HasReturning || HasComplexDynamicHoles || 
            HasSetOperation || HasUnconsumableAlias || HasWindowFunction || HasUpsert;
    }
