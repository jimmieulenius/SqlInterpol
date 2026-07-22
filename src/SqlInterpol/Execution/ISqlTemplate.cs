using SqlInterpol.Configuration;

namespace SqlInterpol;

/// <summary>
/// Represents a high-performance, pre-compiled SQL template that bypasses the standard segment processing 
/// pipeline to deliver raw SQL string generation and O(1) parameter binding.
/// </summary>
public interface ISqlTemplate
{
    /// <summary>
    /// Renders the template into a raw SQL string, injecting extracted arguments directly
    /// into the provided SQL context's parameter dictionary.
    /// </summary>
    /// <param name="context">The active builder context to generate parameter keys for.</param>
    /// <param name="arguments">The input payload containing values to bind.</param>
    /// <returns>A raw SQL string natively formatted for the active dialect.</returns>
    string Render(ISqlContext context, object? arguments = null);
}