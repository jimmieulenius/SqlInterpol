using System.Data;
using System.Data.Common;
using Npgsql;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.E2E.Tests.Framework;
using Xunit;

namespace SqlInterpol.E2E.Tests.Dialects.PostgreSql;

[CollectionDefinition("PostgreSQL E2E", DisableParallelization = true)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}

public class PostgreSqlFixture : E2EDatabaseTestSuiteBase
{
    protected override SqlDialectKind Dialect => SqlDialectKind.PostgreSql;

    protected override IDbConnection CreateConnection() =>
        new NpgsqlConnection(E2EConfig.GetConnectionString(SqlDialectKind.PostgreSql));

    protected override SqlBuilder ConfigureBuilder(SqlInterpolOptions? options = null) =>
        SqlBuilder.PostgreSql(options);

    public override async Task ResetDatabaseAsync()
    {
        if (Connection is null) return;

        using var command = Connection.CreateCommand();
        command.CommandText = """
            DO $$
            DECLARE
                r RECORD;
            BEGIN
                FOR r IN (
                    SELECT nspname
                    FROM pg_namespace
                    WHERE nspname NOT LIKE 'pg_%'
                      AND nspname <> 'information_schema'
                ) LOOP
                    EXECUTE 'DROP SCHEMA IF EXISTS ' || quote_ident(r.nspname) || ' CASCADE';
                END LOOP;
                CREATE SCHEMA public;
            END $$;
            """;

        if (command is DbCommand dbCommand)
        {
            await dbCommand.ExecuteNonQueryAsync();
        }
        else
        {
            command.ExecuteNonQuery();
        }
    }
}