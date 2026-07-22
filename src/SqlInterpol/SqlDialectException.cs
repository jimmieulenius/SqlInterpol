using System;

namespace SqlInterpol;

/// <summary>
/// Thrown when a SQL fragment or segment (representing a baseline PostgreSQL operation) 
/// cannot be transpiled or executed by the currently active ISqlDialect.
/// </summary>
public class SqlDialectException : Exception
{
    /// <summary>
    /// Gets the name of the dialect that rejected the operation.
    /// </summary>
    public string DialectName { get; }

    /// <summary>
    /// Gets the name of the operation, keyword, or fragment that is unsupported.
    /// </summary>
    public string UnsupportedOperation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDialectException"/> class.
    /// </summary>
    /// <param name="dialectName">The name of the dialect.</param>
    /// <param name="unsupportedOperation">The operation that is unsupported.</param>
    public SqlDialectException(string dialectName, string unsupportedOperation) 
        : base($"The SQL dialect '{dialectName}' does not support the operation or fragment type: '{unsupportedOperation}'.")
    {
        DialectName = dialectName;
        UnsupportedOperation = unsupportedOperation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDialectException"/> class with a custom error message.
    /// </summary>
    /// <param name="dialectName">The name of the dialect.</param>
    /// <param name="unsupportedOperation">The operation that is unsupported.</param>
    /// <param name="message">The custom error message.</param>
    public SqlDialectException(string dialectName, string unsupportedOperation, string message) 
        : base(message)
    {
        DialectName = dialectName;
        UnsupportedOperation = unsupportedOperation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDialectException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public SqlDialectException(string message, Exception innerException) 
        : base(message, innerException)
    {
        DialectName = "Unknown";
        UnsupportedOperation = "Unknown";
    }
}