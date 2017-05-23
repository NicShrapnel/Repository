using EntityFramework_Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL_EntityFramework_Repository_Logging;
using System.Data.Common;

namespace EntityFramework_Repository.Models
{
    internal class ChangeLogger : IChangeLogger
    {
        public LoggingEntities ctx { get; set; }
        void IChangeLogger.LoadContext()
        {
            ctx = new LoggingEntities();
        }
        void IChangeLogger.LoadContext(string connectionstring)
        {
            ctx = new LoggingEntities(connectionstring);
        }
        void IChangeLogger.LoadContext(DbConnection connection, bool killContextOnDispose)
        {
            ctx = new LoggingEntities(connection, killContextOnDispose);
        }
        void IChangeLogger.Log(ChangeLog_Entry log)
        {
            ctx.RepositoryChangeLog.Add(new RepositoryChangeLog
            {
                ColumnUpdated = log.ColumnUpdated,
                CommittedAt = log.CommittedAt = DateTime.Now,
                ErrorAt = log.ErrorAt,
                ErrorNotes = log.ErrorNotes,
                NewValue = log.NewValue,
                OldValue = log.OldValue,
                Operation = log.Operation,
                PrimaryKey = log.PrimaryKey,
                TableUpdated = log.TableUpdated,
                RepositoryChangeLogId = log.Entry_ID
            });

            ctx.SaveChanges();
        }
    }
}
