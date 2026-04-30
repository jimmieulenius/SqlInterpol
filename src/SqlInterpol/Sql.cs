namespace SqlInterpol;

public static class Sql
{
    public static ISqlFragment Raw(string? value) => new SqlRawFragment(value ?? string.Empty);
}