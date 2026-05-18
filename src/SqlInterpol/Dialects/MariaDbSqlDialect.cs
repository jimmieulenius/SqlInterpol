using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class MariaDbSqlDialect : MySqlSqlDialect
{
    public override SqlDialectKind Kind => SqlDialectKind.MariaDb; // (You would add this to the enum)

    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate,
        SqlFeature.ForShare,
        SqlFeature.OnConflict,
        SqlFeature.Returning // MariaDB supports this natively!
    };
}