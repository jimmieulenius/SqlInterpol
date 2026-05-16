using SqlInterpol.Metadata;
using SqlInterpol.References;

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

    public static List<ISqlAssignmentFragment> BuildAssignments(ISqlEntityBase entity, object dto)
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
            
            assignments.Add(new SqlAssignmentFragment(reference, value));
        }

        return assignments;
    }
}