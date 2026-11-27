using System.Data;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.E2E.Tests.Framework;

public abstract class E2EDatabaseTestSuiteBase : IAsyncLifetime
{
    private const string InitFolderName = "$Init";

    protected abstract IDbConnection CreateConnection();

    protected abstract SqlBuilder ConfigureBuilder(SqlInterpolOptions? options);

    protected abstract SqlDialectKind Dialect { get; }

    public IDbConnection? Connection { get; private set; }

    public abstract Task ResetDatabaseAsync();

    public Task InitializeAsync()
    {
        try
        {
            Connection = CreateConnection();
            Connection.Open();
        }
        catch (Exception)
        {
            // Database is offline or failed to initialize.
            // Gracefully swallow the exception so xUnit can skip in the test classes.
            Connection = null;
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Connection?.Dispose();
        return Task.CompletedTask;
    }

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
    {
        SqlTestCase.CurrentConnection.Value = Connection;
        return ConfigureBuilder(options);
    }

    public void PrepareTest(string suiteFolder, SqlTestDbSeed? seed = null)
    {
        if (Connection is null) return;

        // 1. Wipe database via dialect-specific reset
        ResetDatabaseAsync().GetAwaiter().GetResult();

        var scriptsRoot = Path.Combine(
            AppContext.BaseDirectory,
            "Dialects",
            Dialect.Value,
            suiteFolder,
            "Db",
            "Scripts");

        // 2. Always run $Init scripts first if present
        if (Directory.Exists(scriptsRoot))
        {
            ExecuteDirectory(Path.Combine(scriptsRoot, InitFolderName));
        }

        if (seed is null || string.IsNullOrWhiteSpace(seed.Value))
            return;

        // 3. Run inline SQL or resolve folder/file path
        if (seed.Kind == SqlSeedKind.Inline)
        {
            ExecuteSql(seed.Value);
            return;
        }

        var targetPath = Path.Combine(scriptsRoot, seed.Value);

        if (Directory.Exists(targetPath))
        {
            ExecuteDirectory(targetPath);
        }
        else if (File.Exists(targetPath))
        {
            ExecuteFile(targetPath);
        }
        else if (File.Exists($"{targetPath}.sql"))
        {
            ExecuteFile($"{targetPath}.sql");
        }
        else
        {
            throw new FileNotFoundException($"E2E seed path not found: {targetPath}");
        }
    }

    private void ExecuteDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        var files = Directory
            .GetFiles(directoryPath, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            ExecuteFile(file);
        }
    }

    private void ExecuteFile(string filePath)
    {
        var sql = File.ReadAllText(filePath);
        ExecuteSql(sql);
    }

    private void ExecuteSql(string sql)
    {
        if (Connection is null || string.IsNullOrWhiteSpace(sql)) return;

        using var command = Connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}