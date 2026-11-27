using System.Data;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.E2E.Tests.Framework;
using Xunit;

namespace SqlInterpol.E2E.Tests.Dialects.Firebird;

[CollectionDefinition("Firebird E2E", DisableParallelization = true)]
public sealed class FirebirdCollection : ICollectionFixture<FirebirdFixture>
{
}

public class FirebirdFixture : E2EDatabaseTestSuiteBase
{
    protected override SqlDialectKind Dialect => SqlDialectKind.Firebird;

    protected override IDbConnection CreateConnection()
    {
        var cs = E2EConfig.GetConnectionString(SqlDialectKind.Firebird);
        try
        {
            using var probe = new FbConnection(cs);
            probe.Open();
        }
        catch (FbException ex) when (ex.ErrorCode == 335544344)
        {
            FbConnection.CreateDatabase(cs, pageSize: 16384, forcedWrites: false, overwrite: false);
        }

        return new FbConnection(cs);
    }

    protected override SqlBuilder ConfigureBuilder(SqlInterpolOptions? options = null) =>
        SqlBuilder.Firebird(options);

    public override async Task ResetDatabaseAsync()
    {
        if (Connection is null) return;

        using var command = Connection.CreateCommand();
        command.CommandText = """
            EXECUTE BLOCK AS
            DECLARE VARIABLE stmt VARCHAR(1024);
            BEGIN
                FOR SELECT 'ALTER TABLE "' || TRIM(r.RDB$RELATION_NAME) || '" DROP CONSTRAINT "' || TRIM(r.RDB$CONSTRAINT_NAME) || '"'
                    FROM RDB$RELATION_CONSTRAINTS r
                    WHERE r.RDB$CONSTRAINT_TYPE = 'FOREIGN KEY'
                INTO :stmt DO
                    EXECUTE STATEMENT :stmt;

                FOR SELECT 'DROP VIEW "' || TRIM(RDB$RELATION_NAME) || '"'
                    FROM RDB$RELATIONS
                    WHERE RDB$VIEW_BLR IS NOT NULL
                      AND (RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0)
                INTO :stmt DO
                    EXECUTE STATEMENT :stmt;

                FOR SELECT 'DROP TABLE "' || TRIM(RDB$RELATION_NAME) || '"'
                    FROM RDB$RELATIONS
                    WHERE RDB$VIEW_BLR IS NULL
                      AND (RDB$SYSTEM_FLAG IS NULL OR RDB$SYSTEM_FLAG = 0)
                INTO :stmt DO
                    EXECUTE STATEMENT :stmt;
            END
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

    protected override void ExecuteSql(string sql)
    {
        if (Connection is not FbConnection fbConnection || string.IsNullOrWhiteSpace(sql))
            return;

        var script = new FbScript(sql);
        script.Parse();

        var batch = new FbBatchExecution(fbConnection);
        batch.AppendSqlStatements(script);
        batch.Execute();
    }
}