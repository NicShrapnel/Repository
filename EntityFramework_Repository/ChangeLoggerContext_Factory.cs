using EntityFramework_Repository.Interfaces;
using EntityFramework_Repository.Models;
using System.Data.Common;

namespace EntityFramework_Repository
{
    internal class ChangeLoggerContext_Factory
    {
        public static IChangeLogger CreateChangeLogger(ConnectionFactory.ConnectionMethod connectionMethod, string connectionstring = null, 
            DbConnection conn = null, bool? killConnectionOnDispose = null)
        {
            IChangeLogger c = new ChangeLogger();
            switch (connectionMethod)
            {
                case ConnectionFactory.ConnectionMethod.AppSettingsConnectionString:
                    {
                        c.LoadContext();
                        break;
                    }
                case ConnectionFactory.ConnectionMethod.ConnectionString:
                    {
                        c.LoadContext(connectionstring);
                        break;
                    }
                case ConnectionFactory.ConnectionMethod.CurrentContextConnection:
                    {
                        bool killconn = false; 
                        if (killConnectionOnDispose.HasValue)
                            killconn = killConnectionOnDispose.Value;

                        c.LoadContext(conn, killconn);
                        break;
                    }
            }

            return c;
        }
    }
}
