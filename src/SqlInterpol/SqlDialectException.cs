namespace SqlInterpol;

/// <summary>
/// Thrown when an AST node (representing a baseline PostgreSQL operation) 
/// cannot be transpiled or executed by the currently active ISqlDialect.
/// </summary>
public class SqlDialectException : Exception
{
    public string DialectName { get; }
    public string UnsupportedOperation { get; }

    public SqlDialectException(string dialectName, string unsupportedOperation) 
        : base($"The SQL dialect '{dialectName}' does not support the operation or node type: '{unsupportedOperation}'.")
    {
        DialectName = dialectName;
        UnsupportedOperation = unsupportedOperation;
    }

    public SqlDialectException(string dialectName, string unsupportedOperation, string message) 
        : base(message)
    {
        DialectName = dialectName;
        UnsupportedOperation = unsupportedOperation;
    }

    public SqlDialectException(string message, Exception innerException) 
        : base(message, innerException)
    {
        DialectName = "Unknown";
        UnsupportedOperation = "Unknown";
    }
}