// using SqlInterpol.Configuration;
// using Xunit.Abstractions;

// namespace SqlInterpol.Testing.Xunit;

// /// <summary>
// /// Provides a base class for xUnit-serializable SQL test cases, managing dialect state and builder instantiation.
// /// </summary>
// public abstract class SqlTestCaseBase : IXunitSerializable
// {
//     /// <summary>
//     /// Gets or sets the SQL dialect kind used for this test case.
//     /// </summary>
//     public SqlDialectKind Dialect { get; protected set; } = default!;

//     /// <summary>
//     /// Gets the active builder instantiated during the test.
//     /// This allows custom assertions to inspect the internal Segment Stream after generation.
//     /// </summary>
//     public SqlBuilder? ActiveBuilder { get; private set; }

//     /// <summary>
//     /// Initializes a new instance of the <see cref="SqlTestCaseBase"/> class.
//     /// Required for xUnit deserialization.
//     /// </summary>
//     public SqlTestCaseBase() { }

//     /// <summary>
//     /// Initializes a new instance of the <see cref="SqlTestCaseBase"/> class with a specific dialect.
//     /// </summary>
//     /// <param name="dialect">The SQL dialect kind to use for testing.</param>
//     protected SqlTestCaseBase(SqlDialectKind dialect)
//     {
//         Dialect = dialect;
//     }

//     /// <summary>
//     /// Creates a new <see cref="SqlBuilder"/> instance using the configured dialect and optional configuration.
//     /// </summary>
//     /// <param name="options">Optional interpolation configuration to apply to the builder.</param>
//     /// <returns>A fully initialized <see cref="SqlBuilder"/> ready for query generation.</returns>
//     public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null)
//     {
//         // Store it so test assertions can access the final state
//         ActiveBuilder = SqlBuilderFactory.Create(Dialect, options);
//         return ActiveBuilder;
//     }

//     /// <summary>
//     /// Serializes the test case state so xUnit can pass it between app domains or runners.
//     /// </summary>
//     /// <param name="info">The xUnit serialization payload container.</param>
//     public virtual void Serialize(IXunitSerializationInfo info)
//     {
//         info.AddValue(nameof(Dialect), Dialect.Value);
//     }

//     /// <summary>
//     /// Deserializes the test case state from the xUnit runner.
//     /// </summary>
//     /// <param name="info">The xUnit serialization payload container.</param>
//     public virtual void Deserialize(IXunitSerializationInfo info)
//     {
//         Dialect = info.GetValue<string>(nameof(Dialect));
//     }
// }