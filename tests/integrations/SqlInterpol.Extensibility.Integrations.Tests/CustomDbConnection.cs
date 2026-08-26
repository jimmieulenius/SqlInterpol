using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace SqlInterpol.Extensibility.Integrations.Tests;

/// <summary>
/// A dummy connection simulating a 3rd party or proprietary database driver.
/// </summary>
public class CustomDbConnection : DbConnection
{
    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "DummyDb";
    public override string DataSource => "DummyServer";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => ConnectionState.Closed;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
    protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
}