Phase 1: The Runtime AOT Infrastructure (The "Glue")
Before touching Roslyn, prepare the runtime SqlBuilder to accept highly optimized, pre-computed string injections.

Update ISqlGeneratorBuilder: Add the hidden infrastructure methods explicitly to the interface so they don't pollute the user's IntelliSense.

void AppendRaw(string rawSql, string? segmentTag = null); (Uses SqlSegmentTag constants for AST synchronization).

string ResolveAlias(string variableName, string defaultTableName, bool wasAutoAliased); (O(1) dictionary lookup respecting runtime options).

void AppendDeclaration(string tableName, string? schema, string variableName, bool wasAutoAliased); (Handles AS [alias]).

Create SqlAotExtensions: Add aggressively inlined helpers for boilerplate logic to keep emitted code tiny.

AppendFormatting(bool isNewLine, int indentLevel) (Handles SqlCollectionLayout).

AppendInsertValues(string[] columns, object?[] values) (Handles anonymous type extraction mapping).

Outcome: Your runtime is now fully prepared to bypass the AST and execute at 700ns.

Phase 2: Source Generator Setup & MSBuild Integration
Create the SqlInterpol.Generators project (targeting netstandard2.0) and wire up the build configuration.

The .targets File: Create a target file that exposes <SqlInterpolDialects> to the Roslyn pipeline via <CompilerVisibleProperty Include="SqlInterpolDialects" />.

The IIncrementalGenerator: Initialize the generator pipeline.

Use context.AnalyzerConfigOptionsProvider to extract the SqlInterpolDialects list from the .csproj. (Defaulting to PostgreSql if missing).

Set up the SyntaxProvider to look for methods containing db.Append or db.AppendLine.

Outcome: The generator knows exactly which dialects the developer intends to support at compile-time.

Phase 3: The Roslyn Metadata Engine
Move your runtime reflection penalty to the build phase.

Build RoslynMetadataHelper: Write a static helper that inspects Roslyn ITypeSymbol objects.

Map the Attributes: Extract [SqlTable], [SqlColumn], and [SqlIgnore] data directly from the syntax tree.

Anonymous Type Detection: Add logic to identify DTOs (new { ... }) so the emitter knows to wire up your existing fast-getters (SqlMetadataRegistry.GetArgumentGetters()).

Outcome: 100% of reflection and custom attribute parsing is permanently eliminated from the runtime path.

Phase 4: The Syntax Walker (Context Tracking)
Write a CSharpSyntaxWalker to safely extract the SQL strings and variables.

Alias Extraction (db.Entity): When the walker sees db.Entity<OrderLine>(out var ol), it captures the variable name ("ol") and whether it relied on [CallerArgumentExpression]. Store this in a CompileTimeQueryContext.

Scope Stacking (db.Query): Implement a Stack<CompileTimeQueryContext>. When the walker sees .Query(..., () => ...), push a new scope. When the lambda ends, pop it.

Fragment Collection: When it sees db.Append(...), extract the literal text and the holes, and record the exact syntax node location for the interceptor attribute.

The JIT Fallback Trigger: If the walker sees db passed into an opaque method or mutated in an untrackable loop, it instantly aborts tracking. The runtime engine flawlessly handles the rest.

Phase 5: The Emitter & Transpiler (Code Generation)
This is where the magic happens. Loop through the collected fragments and emit the [InterceptsLocation] C# files.

Unroll the Metadata: * Replace {ol.Id} with the hardcoded physical column name from RoslynMetadataHelper.

Replace {ol} with a call to builder.AppendDeclaration() or builder.ResolveAlias().

The Multi-Dialect Transpiler Switch: * Run a lightweight regex/lexer over the extracted string looking for transpilation chunks (LIMIT, FOR UPDATE).

Emit a switch (genDb.Context.Dialect.Kind) statement.

Generate the perfect raw string for each dialect found in the SqlInterpolDialects MSBuild list (e.g., OFFSET X ROWS for SQL Server, LIMIT X for Postgres).

Outcome: A completely unrolled, AOT-transpiled query that executes in under a microsecond.

Phase 6: Staged Rollout
Because the JIT fallback is already fully tested, deploy the Source Generator progressively:

Stage 1 (Simple Queries): Get basic SELECT * FROM {ol} working. Run your 4,700ns benchmark and watch it hit 700ns.

Stage 2 (Metadata & Formatting): Implement column mapping ({ol.Id}) and the VALUES {dto} anonymous type expansion.

Stage 3 (Multi-Dialect Transpilation): Implement the MSBuild switch logic for LIMIT and OFFSET.

Stage 4 (Fallback Verification): Run the full 900+ test suite and ensure that highly complex dynamic queries correctly bypass AOT and execute perfectly on the runtime engine.