using SqlInterpol.Configuration;
using SqlInterpol.Execution;
using SqlInterpol.Schema;
using Category = SqlInterpol.Testing.Specifications.SelectSubqueryTestSuite.Category;
using Product = SqlInterpol.Testing.Specifications.SelectSubqueryTestSuite.Product;

namespace SqlInterpol.Testing.Specifications;

public static partial class SelectSubqueryTestHelper
{
    [SqlQuery]
    public static ISqlQuery<Category> BuildCategorySubquery(
        SqlBuilder db,
        Product p,
        int activeStatus)
    {
        return db
            .Entity<Category>(out var c)
            .Query(c, () => db.Append($$"""
                SELECT
                    {{c.Name}}
                FROM {{c}}
                WHERE {{c.Id}} = {{db.Column(p, nameof(p.CategoryId))}} AND {{c.IsActive}} = {{activeStatus}}
                """));
    }
}