using Elsa.Persistence.YesSql.Data;
using YesSql.Sql;

namespace Presentation.Web.Server
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            SchemaBuilder.CreateMapIndexTable<DemoHttpWorkflowContextIndex>(table =>
                table.Column<string>(nameof(DemoHttpWorkflowContextIndex.Uid)));
            return 1;
        }
    }
}