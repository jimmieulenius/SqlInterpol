The Master Execution Plan
Step 1: The API Surface & AST Foundation (Setting up the hidden ISqlGeneratorBuilder interface, the public Sql.* factory methods using PostgreSQL naming, and SqlDialectException).

Step 2: The Runtime Engine (Tier 1) (Refining the fallback SqlSegmentPreprocessor to act strictly as a zero-allocation block-lexer, ignoring deep parsing).

Step 3: Source Generator Pipeline (Tier 2 - Phase A) (Setting up IIncrementalGenerator to find db.Append calls and extract string literals/holes).

Step 4: ANTLR4 Integration (Tier 2 - Phase B) (Wiring the extracted string into the PostgreSQL ANTLR parser and creating the Visitor to map syntax to AST nodes).

Step 5: Emitting the Code (Tier 2 - Phase C) (Generating the C# 12 [InterceptsLocation] code that calls the hidden AppendRaw/AppendNode methods).

Prompt 1: The API Surface & AST Foundation
Use this prompt to establish the safe user boundaries and the Postgres-style AST factories.

---------------------------------------------------------------

Markdown
# Project Context: SqlInterpol
I am building `SqlInterpol`, a high-performance, zero-allocation micro-ORM for .NET. It uses a "Smart Switch" architecture: developers write standard PostgreSQL syntax in C# interpolated strings (`db.Append($"""...""")`). A Roslyn Source Generator (Tier 2) transpiles this to optimized native AST nodes at compile-time for SQL Server, MySQL, etc. Dynamic/unresolvable queries fall back to a fast runtime lexer (Tier 1).

# The Master Plan
[CURRENT] Step 1: The API Surface & AST Foundation
[PENDING] Step 2: The Runtime Engine (Tier 1)
[PENDING] Step 3: Source Generator Pipeline (Tier 2 - Phase A)
[PENDING] Step 4: ANTLR4 Integration (Tier 2 - Phase B)
[PENDING] Step 5: Emitting the Code (Tier 2 - Phase C)

# Step 1 Task: API Boundaries & AST Factories
We need to design the foundational API classes to ensure framework safety and establish our PostgreSQL baseline vocabulary.

Please provide the code for:
1. `ISqlGeneratorBuilder`: An internal-facing interface marked with `[EditorBrowsable(EditorBrowsableState.Never)]`. It should contain the dangerous mutator methods: `AppendRaw(string)`, `AppendNode(ISqlFragment)`, and `AppendTemplate(...)`.
2. `SqlBuilder`: Explicitly implement `ISqlGeneratorBuilder` so users cannot see the raw methods. Expose the safe `Append([InterpolatedStringHandlerArgument]...)` method.
3. `Sql` (Static Class): The public factory for AST nodes. Provide a skeleton of methods using PostgreSQL naming (e.g., `Concat()`, `Coalesce()`, `StringAgg()`, `DateTrunc()`). These methods should return `ISqlFragment`.
4. `SqlDialectException`: A custom exception to be thrown at runtime if the active `ISqlDialect` does not support a specific AST node (e.g., SQL Server throwing on `Sql.RegexMatch()`).

---------------------------------------------------------------

Prompt 2: The Runtime Engine (Tier 1)
Use this prompt to ensure the runtime fallback is allocation-free and respects the dynamic boundaries.

Markdown
# Project Context: SqlInterpol
I am building `SqlInterpol`, a high-performance, zero-allocation micro-ORM for .NET using interpolated strings. We use a hybrid engine: static strings are compiled to AST nodes at build time (Tier 2), while dynamic/unresolvable strings fall back to our runtime engine (Tier 1). The baseline syntax is standard PostgreSQL.

# The Master Plan
[DONE] Step 1: The API Surface & AST Foundation
[CURRENT] Step 2: The Runtime Engine (Tier 1)
[PENDING] Step 3: Source Generator Pipeline (Tier 2 - Phase A)
[PENDING] Step 4: ANTLR4 Integration (Tier 2 - Phase B)
[PENDING] Step 5: Emitting the Code (Tier 2 - Phase C)

# Step 2 Task: The Tier 1 Lexer
The runtime engine must be blazing fast. It should NEVER attempt to parse deep expressions (like `||` or `CONCAT`) to avoid allocations. It should only scan for block-level structural keywords (like `LIMIT`, `OFFSET`, `RETURNING`, `ON CONFLICT`). 

Please provide:
1. The architectural skeleton for `SqlSegmentPreprocessor` and its `Lexer`.
2. Logic demonstrating an O(N) forward-scan that identifies a block keyword (e.g., `LIMIT`), extracts its associated parameters, and maps it to a structural AST node (e.g., `SqlPagingFragment`).
3. An explicit explanation/comment block in the code showing how a raw dynamic string (e.g., containing `||` passed to `AppendRaw`) simply bypasses parsing and goes straight to the database, enforcing the rule that uncompiled dynamic queries must be native SQL.

---------------------------------------------------------------

Prompt 3: Source Generator Pipeline (Tier 2 - Phase A)
Use this prompt to start the Roslyn incremental generator and extract the string literal data.

Markdown
# Project Context: SqlInterpol
I am building `SqlInterpol`, a C# micro-ORM. We are building the Tier 2 compile-time engine: a Source Generator that intercepts `db.Append($"""...""")`, parses the PostgreSQL syntax, and emits highly optimized AST builder code to avoid runtime parsing. 

# The Master Plan
[DONE] Step 1: The API Surface & AST Foundation
[DONE] Step 2: The Runtime Engine (Tier 1)
[CURRENT] Step 3: Source Generator Pipeline (Tier 2 - Phase A)
[PENDING] Step 4: ANTLR4 Integration (Tier 2 - Phase B)
[PENDING] Step 5: Emitting the Code (Tier 2 - Phase C)

# Step 3 Task: IIncrementalGenerator Skeleton
We need to hook into the Roslyn compiler. DO NOT write the SQL parsing or the emitted output yet. Focus purely on the Roslyn pipeline.

Please provide the code for:
1. An `IIncrementalGenerator` class.
2. A `SyntaxProvider` predicate/transform that efficiently hunts for `InvocationExpressionSyntax` where the method being called is `db.Append` or `db.Query`.
3. Logic to extract the `InterpolatedStringExpressionSyntax`.
4. A data model (e.g., `ExtractedQueryInfo`) that holds the literal string chunks and the positions/types of the interpolation holes (e.g., replacing holes with `$1`, `$2` so it forms a valid string we can parse later). Ensure this part of the pipeline is highly performant and caches well.

---------------------------------------------------------------

Prompt 4: ANTLR4 Integration (Tier 2 - Phase B)
Use this prompt to wire up the actual PostgreSQL parsing logic inside the Source Generator.

Markdown
# Project Context: SqlInterpol
I am building `SqlInterpol`, a C# micro-ORM. We are currently working on our Roslyn Source Generator. The generator has already extracted C# interpolated strings (which use standard PostgreSQL syntax). We now need to parse these strings into an AST using the official ANTLR4 PostgreSQL grammar (`Antlr4.Runtime.Standard`).

# The Master Plan
[DONE] Step 1: The API Surface & AST Foundation
[DONE] Step 2: The Runtime Engine (Tier 1)
[DONE] Step 3: Source Generator Pipeline (Tier 2 - Phase A)
[CURRENT] Step 4: ANTLR4 Integration (Tier 2 - Phase B)
[PENDING] Step 5: Emitting the Code (Tier 2 - Phase C)

# Step 4 Task: ANTLR Parser & AST Visitor
Assume we have a valid PostgreSQL string (e.g., `"SELECT FirstName || ' ' || LastName FROM Users LIMIT $1"`). 

Please provide:
1. The C# setup code to pass this string into the ANTLR `PostgreSqlLexer` and `PostgreSqlParser`.
2. A skeleton for a custom `PostgreSqlParserBaseVisitor<ISqlFragment>` (or similar concept for code generation).
3. Implement 2-3 specific visitor methods (e.g., visiting a Concat expression `||`, and visiting a `LIMIT` clause) to demonstrate how the ANTLR AST maps to our C# `Sql.*` factory methods (e.g., emitting the string representation of `Sql.Concat(...)`).

---------------------------------------------------------------

Prompt 5: Emitting the Code (Tier 2 - Phase C)
Use this prompt to generate the final C# 12 Interceptor code that bridges the compiler to the runtime.

Markdown
# Project Context: SqlInterpol
I am building `SqlInterpol`, a C# micro-ORM. We are at the final stage of our Source Generator. We have successfully extracted the C# interpolated string, parsed the PostgreSQL syntax via ANTLR, and generated a list of AST instructions (e.g., `AppendRaw`, `AppendNode(Sql.Concat(...))`). 

# The Master Plan
[DONE] Step 1: The API Surface & AST Foundation
[DONE] Step 2: The Runtime Engine (Tier 1)
[DONE] Step 3: Source Generator Pipeline (Tier 2 - Phase A)
[DONE] Step 4: ANTLR4 Integration (Tier 2 - Phase B)
[CURRENT] Step 5: Emitting the Code (Tier 2 - Phase C)

# Step 5 Task: C# 12 Interceptors
We need to emit the generated C# code back into the user's compilation using C# 12 Interceptors, completely bypassing the runtime Tier 1 engine.

Please provide:
1. The code to emit the `[InterceptsLocation]` attribute (including the necessary polyfill if the user is targeting an older framework but using the C# 12 compiler).
2. A generated static class containing the interceptor method.
3. Demonstrate how the generated method casts the user's `SqlBuilder` to our hidden `ISqlGeneratorBuilder` interface, and executes the sequence of `AppendRaw` and `AppendNode` instructions.
4. Show how the interpolation holes (parameters) are safely passed into this generated method and appended to the builder without allocating arrays.