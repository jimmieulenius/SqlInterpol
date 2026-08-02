// using System.Collections.Concurrent;
// using SqlInterpol.Configuration;

// namespace SqlInterpol.Testing.Xunit;

// /// <summary>
// /// A thread-safe registry that maps SQL dialect names to <see cref="SqlBuilder"/> factory
// /// delegates, enabling xUnit's cross-AppDomain deserialization to reconstruct a dialect-specific
// /// builder from the string identifier stored in <see cref="SqlTestCaseBase.Dialect"/>.
// /// </summary>
// /// <remarks>
// /// All built-in dialects (SqlServer, PostgreSql, MySql, SqLite, Oracle, Firebird) are
// /// pre-registered in the static constructor and available out of the box.
// /// Third-party dialect authors must call <see cref="Register"/> once during test assembly
// /// initialization (e.g. from an <c>ICollectionFixture</c> or assembly-level fixture) so that
// /// deserialized test cases can resolve the correct builder at runtime.
// /// </remarks>
// public static class SqlBuilderFactory
// {
//     // A thread-safe registry of builder factories for the test runner
//     private static readonly ConcurrentDictionary<string, Func<SqlInterpolOptions?, SqlBuilder>> _registry 
//         = new(StringComparer.OrdinalIgnoreCase);

//     /// <summary>
//     /// Statically initialize the built-in dialects so core tests work out-of-the-box.
//     /// </summary>
//     static SqlBuilderFactory()
//     {
//         Register(SqlDialectKind.Firebird.Value, SqlBuilder.Firebird);
//         Register(SqlDialectKind.MySql.Value, SqlBuilder.MySql);
//         Register(SqlDialectKind.Oracle.Value, SqlBuilder.Oracle);
//         Register(SqlDialectKind.PostgreSql.Value, SqlBuilder.PostgreSql);
//         Register(SqlDialectKind.SqLite.Value, SqlBuilder.SqLite);
//         Register(SqlDialectKind.SqlServer.Value, SqlBuilder.SqlServer);
//     }

//     /// <summary>
//     /// Registers a custom dialect builder factory so it can be resolved by xUnit during deserialization.
//     /// Third-party developers MUST call this in their test assembly initialization.
//     /// </summary>
//     /// <param name="dialectName">The unique string identifier for the dialect (which should match <see cref="SqlDialectKind.Value"/>).</param>
//     /// <param name="factory">A delegate that constructs a new <see cref="SqlBuilder"/> instance for this dialect, optionally accepting configuration options.</param>
//     public static void Register(string dialectName, Func<SqlInterpolOptions?, SqlBuilder> factory)
//     {
//         _registry[dialectName] = factory;
//     }

//     /// <summary>
//     /// Creates a new <see cref="SqlBuilder"/> instance for the specified dialect by resolving it from the internal registry.
//     /// </summary>
//     /// <param name="dialect">The kind of SQL dialect to instantiate.</param>
//     /// <param name="options">Optional interpolation configuration to apply to the newly created builder.</param>
//     /// <returns>A fully initialized <see cref="SqlBuilder"/> ready for testing.</returns>
//     /// <exception cref="NotSupportedException">Thrown when the requested dialect has not been registered via <see cref="Register"/>.</exception>
//     public static SqlBuilder Create(SqlDialectKind dialect, SqlInterpolOptions? options = null)
//     {
//         // Dynamically resolve the builder factory from the string identifier!
//         if (_registry.TryGetValue(dialect.Value, out var factory))
//         {
//             return factory(options);
//         }

//         throw new NotSupportedException(
//             $"Unsupported or unregistered SQL dialect: '{dialect.Value}'. " +
//             $"If this is a custom dialect, ensure you call {nameof(SqlBuilderFactory)}.{nameof(Register)}() before the tests run."
//         );
//     }
// }