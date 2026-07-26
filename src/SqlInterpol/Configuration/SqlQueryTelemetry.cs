namespace SqlInterpol.Configuration;

/// <summary>
/// Provides telemetry data for a built SQL query.
/// </summary>
public readonly record struct SqlQueryTelemetry(
    string CallerMemberName, 
    string FilePath,
    int LineNumber,
    bool WasAotIntercepted,
    int ParameterCount,
    TimeSpan BuildDuration
);