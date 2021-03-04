using Elsa.Persistence.YesSql.Data;
using YesSql.Sql;

namespace Presentation.Web.Server
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            this.SchemaBuilder.CreateMapIndexTable<WorkflowStateIndex>(table =>
                table.Column<string>(nameof(WorkflowStateIndex.StateId)));
            return 1;
        }
    }
}