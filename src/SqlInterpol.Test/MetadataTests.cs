using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class MetadataTests
{
    [Theory]
    [MemberData(nameof(MetadataErrorData))]
    public void Metadata_ValidationRules(SqlErrorTestCase testCase)
    {
        // Act
        var exception = Record.Exception(() => 
        {
            string matchingMsg = testCase.ExpectedMessageSubstring;

            if (matchingMsg.Contains("valid property selector"))
            {
                // Triggers internal SqlExpressionHelper.GetMember validation implicitly via GetColumnName
                SqlMetadataRegistry.GetColumnName<Product>(x => x.Id.ToString());
            }
            else if (string.IsNullOrEmpty(matchingMsg))
            {
                SqlMetadataRegistry.GetEntityName(null!);
            }
            else if (matchingMsg.Contains("must implement"))
            {
                SqlMetadataRegistry.GetEntityName(new InvalidDummyEntity());
            }
            else if (matchingMsg.Contains("not found on MetadataErrorModel"))
            {
                SqlMetadataRegistry.GetColumnName<MetadataErrorModel>(x => x.UnmappedProperty);
            }
        });

        // Assert
        testCase.AssertException(exception);
    }

    public static TheoryData<SqlErrorTestCase> MetadataErrorData =>
    [
        new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(ArgumentException), "is not a valid property selector"),
        new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(ArgumentNullException), ""),
        new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(ArgumentException), "must implement ISqlEntityBase<T>"),
        new SqlErrorTestCase(SqlDialectKind.CustomDb, typeof(ArgumentException), "Property 'UnmappedProperty' not found on MetadataErrorModel")
    ];

    private class InvalidDummyEntity : ISqlEntityBase 
    {
        public ISqlReference this[string columnName] => throw new NotImplementedException();

        public ISqlReference Reference => throw new NotImplementedException();

        public ISqlDeclaration Declaration => throw new NotImplementedException();

        public ISqlFragment Column(string name) => throw new NotImplementedException();

        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => throw new NotImplementedException();
    }

    private class MetadataErrorModel
    {
        public int Id { get; set; }
        
        [SqlIgnore]
        public string UnmappedProperty { get; set; } = "";
    }
}