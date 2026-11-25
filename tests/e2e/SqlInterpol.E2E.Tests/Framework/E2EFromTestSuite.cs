// using SqlInterpol.Configuration;
// using SqlInterpol.Testing.Specifications;
// using Xunit;

// namespace SqlInterpol.E2E.Tests.Framework;

// public abstract class E2EFromTestSuite<TFixture>(TFixture fixture) : FromTestSuite, IClassFixture<TFixture> 
//     where TFixture : E2EDatabaseTestSuiteBase
// {
//     public override SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) 
//         => fixture.CreateBuilder(options);
// }