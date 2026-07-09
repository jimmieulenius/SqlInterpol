using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SqlInterpol.Generators;

namespace SqlInterpol.Test.Generators;

public class SqlInterpolGeneratorTests
{
    [Fact]
    public void Generator_Emits_Interceptor_And_AST_Instructions_For_Query()
    {
        // 1. Arrange: The C# code a user would write
        string userSource = @"
using SqlInterpol;

namespace TestApp
{
    public class MyRepository
    {
        public void GetData()
        {
            var builder = SqlBuilder.PostgreSql();
            int id = 42;
            string status = ""Active"";
            
            // This is the call we expect the generator to intercept
            builder.Query($""SELECT * FROM Users WHERE Id = {id} AND Status = {status}"");
        }
    }
}";

        // 2. Setup the Roslyn Compilation
        var compilation = CreateCompilation(userSource);
        
        // 3. Initialize the Source Generator Driver
        var generator = new SqlInterpolGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // 4. Act: Run the generator against our compilation
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // 5. Assert: Check for compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors); // Ensure the generator didn't crash or emit invalid code

        // 6. Inspect the generated files
        var runResult = driver.GetRunResult();
        
        // We expect at least 2 files: InterceptsLocationAttribute.g.cs AND SqlQueryInterceptors.xyz.g.cs
        Assert.True(runResult.GeneratedTrees.Length >= 2, "Expected generated files were not created.");

        // Find the specific file containing the interceptor
        var generatedCode = runResult.GeneratedTrees
            .Select(t => t.ToString())
            .FirstOrDefault(c => c.Contains("class SqlQueryInterceptors"));

        Assert.NotNull(generatedCode);

        // 7. Assert AST Emitting logic
        // It should break the string apart and extract the holes without allocating arrays
        Assert.Contains("genBuilder.AppendRaw(\"SELECT * FROM Users WHERE Id = \");", generatedCode);
        Assert.Contains("genBuilder.AppendNode(Sql.Param(handler.GetArgument(0)));", generatedCode);
        Assert.Contains("genBuilder.AppendRaw(\" AND Status = \");", generatedCode);
        Assert.Contains("genBuilder.AppendNode(Sql.Param(handler.GetArgument(1)));", generatedCode);

        // 8. Assert Interceptor wiring
        Assert.Contains("[InterceptsLocationAttribute(", generatedCode);
    }

    /// <summary>
    /// Helper to create an in-memory Roslyn compilation that includes necessary assembly references.
    /// </summary>
    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // We must provide references to the BCL (mscorlib/System) and your SqlInterpol library
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            
            // This is crucial: it allows Roslyn to recognize `SqlBuilder` and `Sql` in the user code
            MetadataReference.CreateFromFile(typeof(SqlInterpol.SqlBuilder).Assembly.Location)
        };

        return CSharpCompilation.Create(
            assemblyName: "SqlInterpol.GeneratorTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}