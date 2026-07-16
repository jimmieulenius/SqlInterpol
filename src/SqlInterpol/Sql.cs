using System.Linq.Expressions;
using System.Reflection;
using SqlInterpol.Configuration;
using SqlInterpol.Processing;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Provides static helper methods and properties for constructing dialect-aware SQL fragments
/// that can be interpolated directly into a <see cref="SqlBuilder"/> query.
/// </summary>
/// <remarks>
/// Most members on this class produce deferred fragments that are resolved against the active
/// dialect at build time, ensuring the correct quoting style is used regardless of the target database.
/// </remarks>
public static class Sql
{
    /// <summary>
    /// Gets a fragment that renders the dialect's opening identifier-quote character
    /// (e.g., <c>"</c> for PostgreSQL/SQLite, <c>[</c> for SQL Server).
    /// </summary>
    public static ISqlFragment OpenQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    /// <summary>
    /// Gets a fragment that renders the dialect's closing identifier-quote character
    /// (e.g., <c>"</c> for PostgreSQL/SQLite, <c>]</c> for SQL Server).
    /// </summary>
    public static ISqlFragment CloseQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);

    /// <summary>
    /// Emits a single argument placeholder for a template.
    /// </summary>
    /// <param name="name">The exact name of the property or parameter to bind at runtime.</param>
    /// <returns>A fragment representing the argument.</returns>
    public static SqlArgumentFragment Arg(string name) => new(name);

    /// <summary>
    /// Emits a single strongly-typed argument placeholder for a template.
    /// </summary>
    /// <typeparam name="T">The model type containing the target property.</typeparam>
    /// <param name="selector">A lambda expression targeting the property to use as the argument name.</param>
    /// <returns>A fragment representing the typed argument.</returns>
    public static SqlArgumentFragment Arg<T>(Expression<Func<T, object>> selector) => 
        new(SqlExpressionHelper.GetPropertyName(selector));

    /// <summary>
    /// Emits a macro that the processing pipeline expands into structural sequence fragments.
    /// </summary>
    /// <typeparam name="TDto">The data transfer object type to expand into SQL columns and assignments.</typeparam>
    /// <param name="keys">An optional list of property names representing primary keys. The pipeline uses this context to automatically route properties (e.g., excluding these keys from SET clauses).</param>
    /// <returns>An expandable structural fragment.</returns>
    public static SqlExpandable<TDto> Expand<TDto>(params string[] keys) => new(keys);

    /// <summary>
    /// Creates a fragment that wraps the provided value in the dialect's identifier quotes at render time.
    /// </summary>
    /// <param name="value">The raw identifier to quote (e.g., a table or column name).</param>
    /// <returns>A deferred fragment that produces the quoted identifier.</returns>
    public static ISqlFragment Quote(string value) => 
        new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(value));

    /// <summary>
    /// Creates a fragment that emits the provided value verbatim into the SQL output,
    /// bypassing parameterization and identifier quoting.
    /// </summary>
    /// <remarks>
    /// Use this only for trusted, developer-controlled strings (e.g., hard-coded SQL expressions).
    /// Never pass user input to this method to avoid SQL injection vulnerabilities.
    /// </remarks>
    /// <param name="value">The raw SQL string to emit.</param>
    /// <returns>A fragment that renders the raw string unchanged.</returns>
    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);

    /// <summary>
    /// Builds a list of assignment fragments by matching properties on a DTO to the mapped columns on an entity.
    /// </summary>
    /// <param name="entity">The target entity whose column mapping is used to resolve physical column names.</param>
    /// <param name="dto">The data-transfer object whose properties are matched by name against the entity.</param>
    /// <param name="context">The active SQL context supplying options like Enum formatting rules.</param>
    /// <returns>An ordered list of assignment fragments ready to be interpolated into a SET or VALUES clause.</returns>
    /// <exception cref="ArgumentException">Thrown when a DTO property has no matching mapped column on the entity.</exception>
    public static List<ISqlAssignmentFragment> BuildAssignments(ISqlEntityBase entity, object dto, ISqlContext context)
    {
        var properties = SqlMetadataRegistry.GetDtoProperties(dto.GetType());
        var assignments = new List<ISqlAssignmentFragment>(properties.Length);
        
        Type? modelType = null;
        foreach (var i in entity.GetType().GetInterfaces())
        {
            if (i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>) || i.Name.StartsWith("ISqlEntity")))
            {
                modelType = i.GetGenericArguments()[0];
                break;
            }
        }
        
        if (modelType == null) 
            throw new ArgumentException("Entity must implement ISqlEntityBase<T>");
            
        var meta = SqlMetadataRegistry.GetMetadata(modelType);
        var globalEnumFormat = context.Options.EnumFormat;
        
        foreach (var prop in properties)
        {
            var entityMember = meta.Columns.Keys.FirstOrDefault(k => k.Name == prop.Name);
            
            if (entityMember == null)
            {
                throw new ArgumentException($"Property '{prop.Name}' on DTO does not exist on Entity.");
            }
            
            string columnName = meta.Columns[entityMember];
            var reference = new SqlColumnReference(entity.Reference, columnName, prop.Name); 
            var value = prop.GetValue(dto);
            
            if (value != null)
            {
                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (underlyingType.IsEnum)
                {
                    var enumAttr = prop.GetCustomAttribute<SqlEnumFormatAttribute>();
                    var format = enumAttr?.Format ?? globalEnumFormat;
                    
                    if (format == SqlEnumFormat.String)
                    {
                        value = value.ToString();
                    }
                    else
                    {
                        value = Convert.ChangeType(value, Enum.GetUnderlyingType(underlyingType));
                    }
                }
            }
            
            assignments.Add(new SqlAssignmentFragment(reference, value));
        }
        
        return assignments;
    }
}