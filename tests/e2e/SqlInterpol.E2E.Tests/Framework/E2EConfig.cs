using SqlInterpol.Dialects;

namespace SqlInterpol.E2E.Tests.Framework;

public static class E2EConfig
{
    public static string DbUser => Environment.GetEnvironmentVariable("E2E_DB_USER") ?? "postgres";
    public static string DbPassword => Environment.GetEnvironmentVariable("E2E_DB_PASSWORD") ?? "Password123!";
    public static string DbName => Environment.GetEnvironmentVariable("E2E_DB_NAME") ?? "sqlinterpol_test";

    public static string GetConnectionString(SqlDialectKind dialect) => dialect switch
    {
        _ when dialect == SqlDialectKind.Firebird => Environment.GetEnvironmentVariable("ConnectionStrings__Firebird") 
            ?? $"User=sysdba;Password={Environment.GetEnvironmentVariable("E2E_FIREBIRD_PASSWORD") ?? "masterkey"};Database=/firebird/data/test.fdb;DataSource=localhost;Port=3050;Dialect=3;",

        _ when dialect == SqlDialectKind.MySql => Environment.GetEnvironmentVariable("ConnectionStrings__MySql") 
            ?? $"Server=localhost;Port=3306;Database={DbName};Uid=root;Pwd={DbPassword};",

        _ when dialect == SqlDialectKind.Oracle => Environment.GetEnvironmentVariable("ConnectionStrings__Oracle") 
            ?? $"Data Source=localhost:1521/FREEPDB1;User Id=sqlinterpol_user;Password={DbPassword};",

        _ when dialect == SqlDialectKind.PostgreSql => Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql") 
            ?? $"Host=localhost;Port=5432;Database={DbName};Username={DbUser};Password={DbPassword};",

        _ when dialect == SqlDialectKind.SqLite => Environment.GetEnvironmentVariable("ConnectionStrings__SqLite") 
            ?? "Data Source=sqlinterpol_e2e_test.db;Mode=ReadWriteCreate;Cache=Shared;",

        _ when dialect == SqlDialectKind.SqlServer => Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") 
            ?? $"Server=localhost,1433;User Id=sa;Password={DbPassword};TrustServerCertificate=True;",

        _ => throw new NotSupportedException($"Dialect {dialect} is not supported in E2E tests.")
    };
}