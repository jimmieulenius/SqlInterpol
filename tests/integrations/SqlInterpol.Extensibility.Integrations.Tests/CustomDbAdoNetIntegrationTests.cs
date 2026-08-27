using System.Collections; // FIX: For non-generic ICollection and IEnumerator
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using SqlInterpol.AdoNet;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Extensibility.Dialects;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Extensibility.Integrations.Tests;

public class CustomDbAdoNetIntegrationTests : IDisposable
{
    public CustomDbAdoNetIntegrationTests()
    {
        // Arrange: Register the CustomDb dialect for our dummy connection type
        SqlInterpolAdoNetExtensions.ConnectionResolvers.Add(ResolveConnection);
    }

    public void Dispose()
    {
        // Cleanup: Remove the resolver
        SqlInterpolAdoNetExtensions.ConnectionResolvers.Remove(ResolveConnection);
    }

    private ISqlDialect? ResolveConnection(Type type) =>
        type.Name == nameof(CustomDbConnection) ? new CustomDbSqlDialect() : null;

    [Fact]
    public void CreateSqlBuilder_FromAdoNetConnection_ShouldResolve_CustomDbDialect()
    {
        // Arrange
        using var connection = new CustomDbConnection();
        var testCase = new SqlTestCase(
            expectedSql: [
                """
                SELECT <<dbo>>.<<Products>>.<<Id>>
                FROM <<dbo>>.<<Products>>
                WHERE <<dbo>>.<<Products>>.<<Id>> = !!100
                """
            ],
            expectedParameters: [42]
        );

        // Act
        testCase.Act(() => 
        {
            // The ADO.NET extension method automatically resolves CustomDbSqlDialect
            var db = connection.CreateSqlBuilder();
            
            db.Entity<Product>(out var p);
            return db.Append(
                $$"""
                SELECT {{p.Id}}
                FROM {{p}}
                WHERE {{p.Id}} = {{42}}
                """).Build();
        });

        // Assert: Validates AST translation to CustomDb syntax
        testCase.Assert();
    }

    [Fact]
    public void BindParameters_ShouldMapQueryResult_ToDbCommandParameters()
    {
        // Arrange
        using var connection = new CustomDbConnection();
        using var command = new DummyDbCommand();
        var db = connection.CreateSqlBuilder();
        
        db.Entity<Product>(out var p);
        var query = db.Append($"SELECT * FROM {p} WHERE {p.CategoryId} = {1} AND {p.Price} > {10.5m}").Build();

        // Act: Bind the generated parameters to the ADO.NET command
        command.BindParameters(query);

        // Assert
        Assert.Equal(2, command.Parameters.Count);
        
        var p1 = (DummyDbParameter)command.Parameters[0];
        Assert.Equal("!!100", p1.ParameterName);
        Assert.Equal(1, p1.Value);

        var p2 = (DummyDbParameter)command.Parameters[1];
        Assert.Equal("!!101", p2.ParameterName);
        Assert.Equal(10.5m, p2.Value);
    }

    // ─── Lightweight Mocks for Testing Parameter Binding ─────────────────────

    private class DummyDbCommand : DbCommand
    {
        private readonly DbParameterCollection _parameters = new DummyDbParameterCollection();
        
        [AllowNull] // FIX: Matches the base class contract allowing nulls
        public override string CommandText { get; set; } = "";
        
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new DummyDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    }

    private class DummyDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull] public override string ParameterName { get; set; } = "";
        [AllowNull] public override string SourceColumn { get; set; } = "";
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private class DummyDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _list = new();
        public override int Count => _list.Count;
        public override object SyncRoot => this;
        public override int Add(object value) { _list.Add((DbParameter)value); return _list.Count - 1; }
        public override void AddRange(Array values) => _list.AddRange(values.Cast<DbParameter>());
        public override void Clear() => _list.Clear();
        public override bool Contains(object value) => _list.Contains((DbParameter)value);
        public override bool Contains(string value) => _list.Any(p => p.ParameterName == value);
        public override void CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _list.GetEnumerator();
        public override int IndexOf(object value) => _list.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) => _list.FindIndex(p => p.ParameterName == parameterName);
        public override void Insert(int index, object value) => _list.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _list.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _list.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _list.RemoveAll(p => p.ParameterName == parameterName);
        protected override DbParameter GetParameter(int index) => _list[index];
        protected override DbParameter GetParameter(string parameterName) => _list.First(p => p.ParameterName == parameterName);
        protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) 
        { 
            var idx = IndexOf(parameterName); 
            if (idx >= 0) _list[idx] = value; 
        }
    }
}