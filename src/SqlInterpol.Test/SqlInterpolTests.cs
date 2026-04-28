using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class SqlGenerationTests
{
    #region Select_From

    public static IEnumerable<object[]> Select_From_Data()
    {
        yield return new object[] { SqlBuilder.SqlServer(), "SELECT [dbo].[Products].[Name] FROM [dbo].[Products]" };
        yield return new object[] { SqlBuilder.PostgreSql(), "SELECT \"dbo\".\"Products\".\"Name\" FROM \"dbo\".\"Products\"" };
        yield return new object[] { SqlBuilder.MySql(), "SELECT `dbo`.`Products`.`Name` FROM `dbo`.`Products`" };
    }

    [Theory]
    [MemberData(nameof(Select_From_Data))]
    public void Select_From(SqlBuilder db, string expectedSql)
    {
        var (_, p) = db.Entities<Product>();
        db.Append($@"SELECT {p[x => x.ProductName]} FROM {p}");
        
        Assert.Equal(expectedSql, db.Build().Sql);
    }

    #endregion

    #region Select_From_As (Testing the Alias Sniffer)

    public static IEnumerable<object[]> Select_From_As_Data()
    {
        yield return new object[] { SqlBuilder.SqlServer(), "SELECT [prod].[Name] FROM [dbo].[Products] AS prod" };
        yield return new object[] { SqlBuilder.PostgreSql(), "SELECT \"prod\".\"Name\" FROM \"dbo\".\"Products\" AS prod" };
    }

    [Theory]
    [MemberData(nameof(Select_From_As_Data))]
    public void Select_From_As(SqlBuilder db, string expectedSql)
    {
        var (_, p) = db.Entities<Product>();
        db.Append($@"SELECT {p[x => x.ProductName]} FROM {p} AS prod");
        
        Assert.Equal(expectedSql, db.Build().Sql);
    }

    #endregion

    #region Select_From_Join (Testing Multi-Table Aliasing)

    public static IEnumerable<object[]> Select_From_Join_Data()
    {
        yield return new object[] { 
            SqlBuilder.SqlServer(), 
            "SELECT p.[Name], c.[Name] FROM [dbo].[Products] AS p JOIN [dbo].[Categories] AS c ON p.[CategoryId] = c.[Id]" 
        };
    }

    // [Theory]
    // [MemberData(nameof(Select_From_Join_Data))]
    // public void Select_From_Join(SqlBuilder db, string expectedSql)
    // {
    //     var (_, p, c) = db.Entities<Product, Category>();
    //     db.Append($@"SELECT {p[x => x.ProductName]}, {c[x => x.CategoryName]} ")
    //       .Append($@"FROM {p} AS p JOIN {c} AS c ON {p[x => x.CategoryId]} = {c[x => x.Id]}");
        
    //     Assert.Equal(expectedSql, db.Build().Sql);
    // }

    #endregion
}