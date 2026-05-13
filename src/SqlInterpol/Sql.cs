using SqlInterpol.Metadata;
using SqlInterpol.References;

namespace SqlInterpol;

public static class Sql
{
    public static ISqlFragment OpenQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    public static ISqlFragment CloseQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);

    public static ISqlFragment GroupBy(params ISqlFragment[] fragments)
    {
        return new SqlCollectionFragment(fragments);
    }
    
    public static ISqlFragment GroupBy(IEnumerable<ISqlFragment> fragments)
    {
        return new SqlCollectionFragment(fragments);
    }

    public static ISqlFragment Insert(ISqlEntityBase entity, params ISqlAssignmentFragment[] assignments)
    {
        return new SqlInsertFragment(entity, assignments);
    }

    public static ISqlFragment Insert<TEntity, TDto>(ISqlEntityBase<TEntity> entity, TDto dto) 
        where TDto : class
    {
        // Handle the single assignment ambiguity
        if (dto is ISqlAssignmentFragment assignment)
        {
            return Insert(entity, [assignment]);
        }

        var assignments = BuildAssignments(entity, dto);

        return new SqlInsertFragment(entity, assignments);
    }

    public static ISqlFragment InsertValues(params ISqlAssignmentFragment[] assignments)
    {
        return new SqlInsertValuesFragment(assignments);
    }

    public static ISqlFragment InsertValues<TEntity, TDto>(ISqlEntityBase<TEntity> entity, TDto dto) 
        where TDto : class
    {
        var assignments = BuildAssignments(entity, dto);
        
        return new SqlInsertValuesFragment(assignments);
    }

    public static ISqlOrderFragment OrderBy(
        ISqlReference reference, 
        SqlOrderDirection direction = SqlOrderDirection.Asc) 
    {
        return new SqlOrderFragment(reference, direction);
    }

    public static ISqlOrderFragment OrderBy(IEnumerable<ISqlOrderFragment> fragments)
    {
        return new SqlOrderCollectionFragment(fragments);
    }
    
    public static ISqlOrderFragment OrderBy(params ISqlOrderFragment[] fragments)
    {
        return new SqlOrderCollectionFragment(fragments);
    }

    public static ISqlFragment Paging(int limit, int offset = 0)
    {
        return new SqlPagingFragment(limit, offset);
    }

    public static ISqlFragment Quote(string value) => 
        new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(value));

    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);

    public static ISqlAssignmentFragment Set(ISqlReference reference, object? value)
    {
        return new SqlAssignmentFragment(reference, value);
    }

    public static ISqlFragment Update(ISqlEntityBase entity, ISqlAssignmentFragment assignment)
    {
        return new SqlUpdateFragment(entity, [assignment]);
    }

    public static ISqlFragment Update(ISqlEntityBase entity, params ISqlAssignmentFragment[] assignments)
    {
        if (assignments.Length == 0) throw new ArgumentException("Update must have at least one assignment.");

        return new SqlUpdateFragment(entity, assignments);
    }

    public static ISqlFragment Update<TEntity, TDto>(ISqlEntityBase<TEntity> entity, TDto dto) 
        where TDto : class
    {
        if (dto is ISqlAssignmentFragment assignment)
        {
            return Update(entity, [assignment]);
        }

        var assignments = BuildAssignments(entity, dto);

        return new SqlUpdateFragment(entity, assignments);
    }

    public static ISqlFragment UpdateSet(ISqlAssignmentFragment assignment) 
        => assignment;

    public static ISqlFragment UpdateSet(params ISqlAssignmentFragment[] assignments) 
        => new SqlCollectionFragment(assignments);

    public static ISqlFragment UpdateSet<TEntity, TDto>(ISqlEntityBase<TEntity> entity, TDto dto) 
        where TDto : class
    {
        if (dto is ISqlAssignmentFragment assignment) return assignment;

        var assignments = BuildAssignments(entity, dto);
        return new SqlCollectionFragment(assignments);
    }

    // Replaces private BuildAssignmentsFromDto
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