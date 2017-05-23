namespace DAL_EntityFramework_Repository_Logging
{
    using System;
    using System.Data.Common;
    using System.Data.Entity;

    public class LoggingEntities : DbContext
    {
        // Your context has been configured to use a 'LoggingModel' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'DAL_EntityFramework_Repository_Logging.LoggingModel' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'LoggingModel' 
        // connection string in the application configuration file.
        public LoggingEntities()
            : base("name=LoggingEntities") { }
        public LoggingEntities(string connectionString)
            : base(connectionString) { }
        public LoggingEntities(DbConnection conn, bool killConnectionOnDispose)
            : base(conn, killConnectionOnDispose) { }

        // Add a DbSet for each entity type that you want to include in your model. For more information 
        // on configuring and using a Code First model, see http://go.microsoft.com/fwlink/?LinkId=390109.

        public virtual DbSet<RepositoryChangeLog> RepositoryChangeLog { get; set; }
    }

    public class RepositoryChangeLog
    {
        public long RepositoryChangeLogId { get; set; }
        public string TableUpdated { get; set; }
        public string PrimaryKey { get; set; }
        public string ColumnUpdated { get; set; }
        public string NewValue { get; set; }
        public string OldValue { get; set; }
        public string Operation { get; set; }
        public DateTime? CommittedAt { get; set; }
        public DateTime? ErrorAt { get; set; }
        public string ErrorNotes { get; set; }
    }
}