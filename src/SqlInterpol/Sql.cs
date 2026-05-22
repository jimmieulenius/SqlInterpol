using System.Reflection;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public static class Sql
{
    public static ISqlFragment OpenQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    public static ISqlFragment CloseQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);

    public static ISqlFragment Quote(string value) => 
        new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(value));

    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);

    // FIX: Pass in the ISqlParserContext so we can read the global EnumFormat setting
    public static List<ISqlAssignmentFragment> BuildAssignments(ISqlEntityBase entity, object dto, ISqlParserContext context)
    {
        var properties = SqlMetadataRegistry.GetDtoProperties(dto.GetType());
        var assignments = new List<ISqlAssignmentFragment>(properties.Length);

        // Safely extract the generic T from ISqlEntityBase<T>
        Type? modelType = null;
        foreach (var i in entity.GetType().GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>))
            {
                modelType = i.GetGenericArguments()[0];
                break;
            }
        }

        if (modelType == null) throw new ArgumentException("Entity must implement ISqlEntityBase<T>");

        var meta = SqlMetadataRegistry.GetMetadata(modelType);
        var globalEnumFormat = context.Options?.EnumFormat ?? SqlEnumFormat.Integer;

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

            // --- ENUM FORMATTING LOGIC ---
            if (value != null)
            {
                var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (underlyingType.IsEnum)
                {
                    // Check for property-level override, fallback to global setting
                    var enumAttr = prop.GetCustomAttribute<SqlEnumFormatAttribute>();
                    var format = enumAttr?.Format ?? globalEnumFormat;

                    if (format == SqlEnumFormat.String)
                    {
                        value = value.ToString();
                    }
                    else
                    {
                        // Safely cast to the underlying integer/byte value
                        value = Convert.ChangeType(value, Enum.GetUnderlyingType(underlyingType));
                    }
                }
            }
            
            assignments.Add(new SqlAssignmentFragment(reference, value));
        }

        return assignments;
    }
}