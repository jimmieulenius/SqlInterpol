namespace SqlInterpol;

/// <summary>
/// Thrown when a query uses a SQL feature that is not supported by the active dialect,
/// or when dialect-capability validation fails during the build phase.
/// </summary>
/// <param name="message">A message describing the unsupported feature or validation failure.</param>
public class SqlDialectException(string message) : Exception(message)
{
}