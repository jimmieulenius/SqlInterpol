using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class OptionsTestSuite
{
    [SqlTable("Users", "dbo")]
    public class StatusEntity
    {
        public TestStatus Status { get; set; }
    }
}