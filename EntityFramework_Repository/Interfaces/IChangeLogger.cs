using EntityFramework_Repository.Models;
using System.Data.Common;

namespace EntityFramework_Repository.Interfaces
{
    interface IChangeLogger
    {
        DAL_EntityFramework_Repository_Logging.LoggingEntities ctx { get; set; }

        void LoadContext();
        void LoadContext(string connectionString);
        void LoadContext(DbConnection connection, bool killContextOnDispose);
        void Log(ChangeLog_Entry log);
    }
}
