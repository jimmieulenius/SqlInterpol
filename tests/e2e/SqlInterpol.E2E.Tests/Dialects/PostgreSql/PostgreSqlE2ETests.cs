using System.Data;
using Npgsql;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.E2E.Tests.Framework;
using SqlInterpol.Testing.Xunit;
using SqlInterpol.Tests.Dialects.PostgreSql;
using Xunit;

namespace SqlInterpol.E2E.Tests.Dialects.PostgreSql;

public class PostgreSqlFixture : E2EDatabaseTestSuiteBase
{
    protected override SqlDialectKind Dialect => SqlDialectKind.PostgreSql;

    protected override IDbConnection CreateConnection() => 
        new NpgsqlConnection(E2EConfig.GetConnectionString(SqlDialectKind.PostgreSql));

    // Implement ConfigureBuilder (base class exposes the public CreateBuilder)
    protected override SqlBuilder ConfigureBuilder(SqlInterpolOptions? options = null) => 
        SqlBuilder.PostgreSql(options);
}

[Trait("Category", "PostgreSQL")]
[Trait("Category", "E2E")]
public partial class PostgreSqlFromTestSuite : PostgreSqlFromTestSuiteBase, IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlFromTestSuite(PostgreSqlFixture fixture)
    {
        _fixture = fixture;

        SqlTestCase.CurrentConnection.Value = _fixture.Connection;
    }

    // Call CreateBuilder on the fixture, which securely injects the CurrentConnection state
    public override SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) 
        => _fixture.CreateBuilder(options);
}