using EntityFramework_Repository.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Common;
using EntityFramework_Repository.Interfaces;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data.Entity.Core.EntityClient;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EntityFramework_Repository
{
    public class Repository : IDisposable
    {
        public IEnumerable<Change_Entry> ChangeLogQueue
        {
            get
            {
                var op = OperationType.NotSet;
                return _ChangeLogQueue.Select(c => new Change_Entry
                {
                    Entry_ID = c.Entry_ID,
                    TableUpdated = c.TableUpdated,
                    PrimaryKey = c.PrimaryKey,
                    ColumnUpdated = c.ColumnUpdated,
                    OldValue = c.OldValue,
                    NewValue = c.NewValue,
                    CommittedAt = c.CommittedAt,
                    ErrorAt = c.ErrorAt,
                    ErrorNotes = c.ErrorNotes,
                    Operation = c.Operation
                }).ToList();
            }
        }

        private List<ChangeLog_Entry> _ChangeLogQueue;
        private List<ContextChange_DTO> ContextChangesQueue;
        private DbConnection ConnectionToUse;
        private string ConnectionString;
        private MetadataWorkspace Workspace;

        public DbContext ctx { get; set; }
        public bool KillConnectionOnDispose;

        IChangeLogger logger;

        public Repository()
        {
            _ChangeLogQueue = new List<ChangeLog_Entry>();
            ContextChangesQueue = new List<ContextChange_DTO>();

            KillConnectionOnDispose = true;

            ctx = null;
        }
        public Repository(DbContext context, ConnectionFactory.ConnectionMethod conn, string connectionString = null,
            bool killConnectionOnCtxDispose = false, DbConnection changeLogConnectionContext = null)
        {
            _ChangeLogQueue = new List<ChangeLog_Entry>();
            ContextChangesQueue = new List<ContextChange_DTO>();

            KillConnectionOnDispose = killConnectionOnCtxDispose;

            ctx = context;
            ConnectionString = ctx.Database.Connection.ConnectionString;
            Workspace = ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace;

            LoadLogger(conn, connectionString, killConnectionOnCtxDispose, changeLogConnectionContext);
        }

        public void LoadContext(DbContext context)
        {
            if (ctx == null)
            {
                ctx = context;
                ConnectionString = ctx.Database.Connection.ConnectionString;
                Workspace = ((IObjectContextAdapter)ctx).ObjectContext.MetadataWorkspace;
            }
        }
        public void LoadLogger(ConnectionFactory.ConnectionMethod conn, string connectionString = null,
            bool killConnectionOnCtxDispose = false, DbConnection changeLogConnectionContext = null)
        {
            DbConnection connToUse = changeLogConnectionContext;

            if (connToUse == null && ctx != null)
                connToUse = ctx.Database.Connection;

            if (!String.IsNullOrWhiteSpace(connectionString) || conn == ConnectionFactory.ConnectionMethod.AppSettingsConnectionString || connToUse != null)
                logger = ChangeLoggerContext_Factory.CreateChangeLogger(conn, connectionString, connToUse, killConnectionOnCtxDispose);

            KillConnectionOnDispose = killConnectionOnCtxDispose;
        }

        public bool CommitContextChanges()
        {
            if (ctx == null)
                return false;

            CheckDisposedAndKillOnDispose();

            try
            {
                foreach (var i in ContextChangesQueue.Where(c => c.Operation == OperationType.Insert && !c.IsCommitted))
                {
                    var logList = _ChangeLogQueue.Where(q => q.Entry_ID >= i.InitialChangeLogID && q.Entry_ID <= i.EndingChangeLogID);

                    AddToContext(i.Entity);

                    foreach (var l in logList)
                    {
                        l.PrimaryKey = GetKeyValues(GetKeyNames(i.Entity.GetType()), i.Entity).FirstOrDefault().Value.ToString();
                        logger.Log(l);
                    }

                    i.IsCommitted = true;
                }
                foreach (var u in ContextChangesQueue.Where(c => c.Operation == OperationType.Update && !c.IsCommitted))
                {
                    var logList = _ChangeLogQueue.Where(q => q.Entry_ID >= u.InitialChangeLogID && q.Entry_ID <= u.EndingChangeLogID);

                    foreach (var l in logList)
                    {
                        l.PrimaryKey = GetKeyValues(GetKeyNames(u.Entity.GetType()), u.Entity).FirstOrDefault().Value.ToString();
                        logger.Log(l);
                    }
                    UpdateContext(u.Entity);

                    u.IsCommitted = true;
                }
                ctx.SaveChanges();
            }
            catch (Exception blargh)
            {
                return false;
            }
            return true;
        }
        private void AddToContext<T>(T entry) where T : class
        {
            CheckDisposedAndKillOnDispose();

            var type = entry.GetType();

            var dbset = ctx.Set(type);
            dbset.Add(entry);
            ctx.SaveChanges();
        }
        private void UpdateContext<T>(T entry) where T : class
        {
            CheckDisposedAndKillOnDispose();

            var type = entry.GetType();
            var dbset = ctx.Set(type);
            //dbset.Add(entry);
            dbset.Attach(entry);
            ctx.Entry(entry).State = EntityState.Modified;

            ctx.SaveChanges();
        }

        internal void QueueChangeLogEntry(ChangeLog_Entry cl)
        {
            _ChangeLogQueue.Add(cl);
        }
        public void QueueContextChange<T>(T entry) where T : class
        {
            if (entry == null || ctx == null)
                return;

            //var entry = Clone<T>(original);

            CheckDisposedAndKillOnDispose();

            if (ChangeLog_Entry.GetID_Counter() == 0)
            {
                long? max = null;
                if (logger.ctx.RepositoryChangeLog.Select(l => l.RepositoryChangeLogId).Count() > 0)
                    max = logger.ctx.RepositoryChangeLog.Max(m => m.RepositoryChangeLogId);

                if (max.HasValue && max > 0)
                    ChangeLog_Entry.SetID_Counter(max.Value);
                else
                    ChangeLog_Entry.SetID_Counter(0);
            }

            var operation = GetOperationType(entry);

            var entity = new ContextChange_DTO
            {
                Entity = entry,
                Operation = operation,
                IsCommitted = false
            };

            var type = entry.GetType();
            var tableName = Repository_Helper.GetTableName(type, ctx);
            var props = type.GetProperties();
            var first = props.First();
            var last = props.Last();

            if (operation == OperationType.Insert)
            {
                foreach (var prop in props)
                {
                    var val = prop.GetValue(entry);

                    if (val == null)
                    {
                        if (entity.EndingChangeLogID == 0 && prop == last)
                        {
                            var changeLogEntry = new ChangeLog_Entry();
                            entity.EndingChangeLogID = changeLogEntry.Entry_ID;
                        }
                        continue;
                    }
                    else
                    {

                        var changeLogEntry = new ChangeLog_Entry
                        {
                            FQAType = entry.GetType().AssemblyQualifiedName,
                            TableUpdated = tableName,
                            PrimaryKey = null,
                            ColumnUpdated = Repository_Helper.GetColumnName(type, ctx, prop.Name),
                            NewValue = val.ToString(),
                            OldValue = null,
                            Operation = "Insert",
                            CommittedAt = null,
                            ErrorAt = null,
                            ErrorNotes = null
                        };

                        if (entity.InitialChangeLogID == 0 && prop == first)
                            entity.InitialChangeLogID = changeLogEntry.Entry_ID;
                        else if (entity.EndingChangeLogID == 0 && prop == last)
                            entity.EndingChangeLogID = changeLogEntry.Entry_ID;

                        QueueChangeLogEntry(changeLogEntry);
                    }
                }
            }
            else if (operation == OperationType.Update)
            {
                var comparison = CheckAlreadyQueued(entry);

                if (comparison == null)
                {
                    var dbset = ctx.Set(type);
                    comparison = dbset.Find(GetKeyValues(GetKeyNames(type), entry).Select(kv => kv.Value).ToArray());
                }

                foreach (var prop in props)
                {
                    object compOldValue = null;
                    if (comparison != null)
                        compOldValue = prop.GetValue(comparison);

                    if (comparison != null && prop.GetValue(entry) != prop.GetValue(comparison))
                    {
                        var changeLogEntry = new ChangeLog_Entry
                        {
                            FQAType = entry.GetType().AssemblyQualifiedName,
                            TableUpdated = tableName,
                            PrimaryKey = null,
                            ColumnUpdated = Repository_Helper.GetColumnName(type, ctx, prop.Name),
                            NewValue = prop.GetValue(entry) == null ? null : prop.GetValue(entry).ToString(),
                            OldValue = compOldValue == null ? null : compOldValue.ToString(),
                            Operation = "Update",
                            CommittedAt = null,
                            ErrorAt = null,
                            ErrorNotes = null
                        };

                        if (entity.InitialChangeLogID == 0 && prop == first)
                            entity.InitialChangeLogID = changeLogEntry.Entry_ID;
                        else if (entity.EndingChangeLogID == 0 && prop == last)
                            entity.EndingChangeLogID = changeLogEntry.Entry_ID;

                        QueueChangeLogEntry(changeLogEntry);
                    }
                }
            }

            ContextChangesQueue.Add(entity);
        }
        private object CheckAlreadyQueued<T>(T entry) //, string fkName, string fKey)
        {
            CheckDisposedAndKillOnDispose();

            var keyNames = GetKeyNames(entry.GetType());
            var keyValues = GetKeyValues(keyNames, entry);

            var result = ContextChangesQueue.Select(q => q.Entity)
                                            .Where(
                                                e => e.GetType().GetProperties()
                                                .Join(keyValues,
                                                p => p.Name,
                                                k => k.Key,
                                                (p, k) => new { Result = p.GetValue(e), Comparison = k.Value }).Where(r => r.Result == r.Comparison)
                                            != null);

            return result.FirstOrDefault();
        }
        private OperationType GetOperationType(object entry)
        {
            if (ctx == null)
                return OperationType.Failed;

            CheckDisposedAndKillOnDispose();

            var type = entry.GetType();
            var dbset = ctx.Set(type);
            object ctxVersion = null;

            try
            {
                var keyvalues = GetKeyValues(GetKeyNames(type), entry);
                ctxVersion = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray());
            }
            catch (ArgumentOutOfRangeException)
            {
                //eat
            }

            if (ctxVersion != null)
                return OperationType.Update;
            else if (CheckAlreadyQueued(entry) != null)
                return OperationType.Update;
            else
                return OperationType.Insert;
        }
        private IEnumerable<string> GetKeyNames(Type type)
        {
            if (ctx == null || type == null)
                return null;

            CheckDisposedAndKillOnDispose();

            return ((IObjectContextAdapter)ctx)
                    .ObjectContext
                    .MetadataWorkspace
                    .GetItem<EntityType>(type.FullName, DataSpace.OSpace)
                    .KeyProperties
                    .Select(p => p.Name);
        }
        private IEnumerable<string> GetDatabaseKeyNames(Type type)
        {
            if (ctx == null || type == null)
                return null;

            CheckDisposedAndKillOnDispose();

            return ((IObjectContextAdapter)ctx)
                    .ObjectContext
                    .MetadataWorkspace
                    .GetItem<EntityType>(type.FullName, DataSpace.SSpace)
                    .KeyProperties
                    .Select(p => p.Name);
        }
        private IDictionary<string, object> GetKeyValues(IEnumerable<string> keyNames, object entity)
        {
            if (ctx == null || keyNames == null || keyNames.Count() == 0)
                return null;

            CheckDisposedAndKillOnDispose();

            var keyValueList = new Dictionary<string, object>();

            foreach (var n in keyNames)
            {
                keyValueList.Add(n, entity.GetType().GetProperty(n).GetValue(entity));
            }

            return keyValueList;
        }

        public bool RemoveFromQueueByID(long ChangeLog_Entry_ID)
        {
            var change = _ChangeLogQueue.Where(c => c.Entry_ID == ChangeLog_Entry_ID).FirstOrDefault();

            var e = ContextChangesQueue
                .Where(q => q.InitialChangeLogID <= ChangeLog_Entry_ID && q.EndingChangeLogID >= ChangeLog_Entry_ID).FirstOrDefault();

            if (e == null)
                return false;

            if (e.InitialChangeLogID == e.EndingChangeLogID)
                ContextChangesQueue.Remove(e);
            else
            {
                var type = e.Entity.GetType();
                var propType = type.GetProperty(Repository_Helper.GetPropertyName(type, ctx, change.ColumnUpdated)).PropertyType;
                type.GetProperty(change.ColumnUpdated).SetValue(e.Entity, Convert.ChangeType(change.OldValue, propType));
            }

            return _ChangeLogQueue.Remove(change);
        }
        public bool RemoveRangeFromQueue(long Beginning_ID, long Ending_ID)
        {
            var listToRemove = _ChangeLogQueue.Where(c => c.Entry_ID >= Beginning_ID && c.Entry_ID <= Ending_ID).ToList();
            var entitiesToModify = ContextChangesQueue
                .Where(q => !q.IsCommitted &&
                     ((q.InitialChangeLogID >= Beginning_ID && q.InitialChangeLogID <= Ending_ID)
                    || (q.EndingChangeLogID >= Beginning_ID && q.EndingChangeLogID >= Ending_ID)))
                .ToList();

            var entitiesToRemoveFromQueue = new List<ContextChange_DTO>();

            foreach (var e in entitiesToModify)
            {
                var type = e.Entity.GetType();
                var entityChangeList = listToRemove.Where(r => r.Entry_ID >= e.InitialChangeLogID && r.Entry_ID <= e.EndingChangeLogID).ToList();

                var dbset = ctx.Set(type);
                var keynames = GetKeyNames(type);
                var numKeysInChange = entityChangeList
                    .Join(keynames,
                    a => Repository_Helper.GetPropertyName(type, ctx, a.ColumnUpdated),
                    k => k,
                    (a, k) => a
                    ).Count();

                if (numKeysInChange == keynames.Count() && (e.InitialChangeLogID >= Beginning_ID || e.EndingChangeLogID <= Ending_ID))
                {
                    entitiesToRemoveFromQueue.Add(e);
                }
                else
                {
                    foreach (var change in entityChangeList)
                    {
                        var propType = type.GetProperty(Repository_Helper.GetPropertyName(type, ctx, change.ColumnUpdated)).PropertyType;
                        type.GetProperty(Repository_Helper.GetPropertyName(type, ctx, change.ColumnUpdated))
                            .SetValue(e.Entity, Convert.ChangeType(change.OldValue, propType));
                    }
                }
                ContextChangesQueue = ContextChangesQueue.Except(entitiesToRemoveFromQueue).ToList();
                _ChangeLogQueue = _ChangeLogQueue.Except(entityChangeList).ToList();
            }

            return true;
        }
        public bool RemoveAllFromQueue()
        {
            if (_ChangeLogQueue.Count == 0)
                return false;

            try
            {
                _ChangeLogQueue.Clear();
                ContextChangesQueue.Clear();
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool RollBackAllChanges()
        {
            CheckDisposedAndKillOnDispose();

            if (ContextChangesQueue.Where(q => q.IsCommitted).Count() == 0)
                return false;

            foreach (var item in ContextChangesQueue.Where(q => q.IsCommitted && q.Operation == OperationType.Update))
            {
                var type = item.Entity.GetType();
                var dbset = ctx.Set(type);

                var keyvalues = GetKeyValues(GetKeyNames(type), item.Entity);
                var obj = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray());

                var changesList = _ChangeLogQueue.Where(c => c.Entry_ID >= item.InitialChangeLogID && c.Entry_ID <= item.EndingChangeLogID);

                if (changesList.Count() == obj.GetType().GetProperties().Count())
                {
                    foreach (var c in changesList)
                    {
                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }
                    dbset.Remove(obj);
                    ctx.SaveChanges();
                }
                else
                {
                    foreach (var c in changesList)
                    {
                        var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                        var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                        obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }

                    UpdateContext(obj);
                }
                logger.ctx.SaveChanges();
            }

            foreach (var item in ContextChangesQueue.Where(q => q.IsCommitted && q.Operation == OperationType.Insert))
            {
                var type = item.Entity.GetType();
                var dbset = ctx.Set(type);

                var keyvalues = GetKeyValues(GetKeyNames(type), item.Entity);
                var obj = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray().ToArray());

                var changesList = _ChangeLogQueue.Where(c => c.Entry_ID >= item.InitialChangeLogID && c.Entry_ID <= item.EndingChangeLogID).ToList();

                foreach (var c in changesList)
                {
                    var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                    if (logEntry != null)
                        logEntry.Operation = "Rolled Back";
                }

                dbset.Remove(obj);
                ctx.SaveChanges();

                logger.ctx.SaveChanges();
            }
            return true;
        }
        public bool RollBackChangesByRange(long Beginning_ID, long Ending_ID)
        {
            CheckDisposedAndKillOnDispose();

            var changeLogRangeMatchingUpdateList = ContextChangesQueue.Where(q => q.IsCommitted && q.Operation == OperationType.Update
            && (q.InitialChangeLogID >= Beginning_ID && q.EndingChangeLogID <= Ending_ID)).ToList();

            var changeLogRangeMatchingInsertList = ContextChangesQueue.Where(q => q.IsCommitted && q.Operation == OperationType.Insert
            && (q.InitialChangeLogID >= Beginning_ID && q.EndingChangeLogID <= Ending_ID)).ToList();

            foreach (var item in changeLogRangeMatchingUpdateList)
            {
                var type = item.Entity.GetType();
                var dbset = ctx.Set(type);
                var keynames = GetKeyNames(type);
                var keyvalues = GetKeyValues(keynames, item.Entity);
                var obj = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray());

                var changesList = _ChangeLogQueue.Where(c => c.Entry_ID >= item.InitialChangeLogID && c.Entry_ID <= item.EndingChangeLogID).ToList();

                foreach (var c in changesList)
                {
                    var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                    var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                    obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                    var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                    if (logEntry != null)
                        logEntry.Operation = "Rolled Back";
                }

                UpdateContext(obj);

                logger.ctx.SaveChanges();
            }

            foreach (var item in changeLogRangeMatchingInsertList)
            {
                var type = item.Entity.GetType();
                var dbset = ctx.Set(type);

                var keyvalues = GetKeyValues(GetKeyNames(type), item.Entity);
                var obj = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray());

                var changesList = _ChangeLogQueue.Where(c => c.Entry_ID >= item.InitialChangeLogID && c.Entry_ID <= item.EndingChangeLogID).ToList();

                var keynames = GetKeyNames(type);
                var numKeysInChange = changesList
                    .Join(keynames,
                    a => Repository_Helper.GetPropertyName(type, ctx, a.ColumnUpdated),
                    k => k,
                    (a, k) => a
                    ).Count();

                if (numKeysInChange == keynames.Count() && (item.InitialChangeLogID >= Beginning_ID || item.EndingChangeLogID <= Ending_ID))
                {
                    foreach (var c in changesList)
                    {
                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }
                    dbset.Remove(obj);
                    ctx.SaveChanges();
                }
                else
                {
                    foreach (var c in changesList)
                    {
                        var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                        var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                        obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }

                    UpdateContext(obj);
                }

                logger.ctx.SaveChanges();
            }

            if (changeLogRangeMatchingInsertList.ToList().Count == 0 && changeLogRangeMatchingUpdateList.ToList().Count == 0)
                return false;

            return true;
        }
        public bool RollBackChangeByID(long ChangeLog_Entry_ID)
        {
            CheckDisposedAndKillOnDispose();

            foreach (var item in ContextChangesQueue.Where(q => q.IsCommitted && (q.InitialChangeLogID <= ChangeLog_Entry_ID && q.EndingChangeLogID >= ChangeLog_Entry_ID)))
            {
                var type = item.Entity.GetType();
                var dbset = ctx.Set(type);

                var keyvalues = GetKeyValues(GetKeyNames(type), item.Entity);
                var obj = dbset.Find(keyvalues.Select(kv => kv.Value).ToArray());

                var changesList = _ChangeLogQueue.Where(c => c.Entry_ID == ChangeLog_Entry_ID).ToList();

                if (changesList.Count() == 0)
                    return false;

                foreach (var c in changesList)
                {
                    var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                    var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                    obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                    var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.Entry_ID).FirstOrDefault();

                    if (logEntry != null)
                        logEntry.Operation = "Rolled Back";
                }

                UpdateContext(obj);

                logger.ctx.SaveChanges();
            }
            return true;
        }

        public IEnumerable<Change_Entry> GetAllChanges_ThisEntity<T>(T entity) where T : class
        {
            CheckDisposedAndKillOnDispose();

            var type = entity.GetType();
            var dbset = ctx.Set(type);

            var keynames = GetKeyNames(type).ToList();
            var keys = GetKeyValues(keynames, entity);

            List<Change_Entry> changeList = new List<Change_Entry>();
            OperationType op = OperationType.NotSet;

            var keyType = keys.FirstOrDefault().GetType();

            foreach (var eKey in keys)
            {
                changeList.AddRange((logger.ctx.RepositoryChangeLog.Where(c => c.TableUpdated == type.Name
                        && c.PrimaryKey == eKey.Value.ToString())
                        .Select(c => new Change_Entry
                        {
                            Entry_ID = c.RepositoryChangeLogId,
                            TableUpdated = c.TableUpdated,
                            PrimaryKey = c.PrimaryKey,
                            ColumnUpdated = Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated),
                            OldValue = c.OldValue,
                            NewValue = c.NewValue,
                            CommittedAt = c.CommittedAt,
                            ErrorAt = c.ErrorAt,
                            ErrorNotes = c.ErrorNotes,
                            Operation = c.Operation
                        }))
                        .ToList()
                    );

                changeList.AddRange((_ChangeLogQueue.Where(c => c.TableUpdated == type.Name
                        && c.PrimaryKey == eKey.Value.ToString())
                        .Select(c => new Change_Entry
                        {
                            Entry_ID = c.Entry_ID,
                            TableUpdated = c.TableUpdated,
                            PrimaryKey = c.PrimaryKey,
                            ColumnUpdated = Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated),
                            OldValue = c.OldValue,
                            NewValue = c.NewValue,
                            CommittedAt = c.CommittedAt,
                            ErrorAt = c.ErrorAt,
                            ErrorNotes = c.ErrorNotes,
                            Operation = c.Operation
                        }))
                        .ToList()
                    );
            }

            return changeList;
        }
        public bool RollBackChanges_Made_By_Previous_Repo_ByRange(long Beginning_ID, long Ending_ID)
        {
            CheckDisposedAndKillOnDispose();

            var changeLogRangeMatchingUpdateList = logger.ctx.RepositoryChangeLog.Where(q => q.Operation == "Update"
            && (q.RepositoryChangeLogId >= Beginning_ID && q.RepositoryChangeLogId <= Ending_ID)).ToList();

            var changeLogRangeMatchingInsertList = logger.ctx.RepositoryChangeLog.Where(q => q.Operation == "Insert"
            && (q.RepositoryChangeLogId >= Beginning_ID && q.RepositoryChangeLogId <= Ending_ID)).ToList();

            string changeGroupKeyValue = null;
            foreach (var entry in changeLogRangeMatchingUpdateList)
            {
                if (!String.IsNullOrWhiteSpace(changeGroupKeyValue) && changeGroupKeyValue == entry.PrimaryKey)
                    continue;

                var type = Type.GetType(entry.TableUpdated);
                var dbset = ctx.Set(type);

                var keyname = GetKeyNames(type).FirstOrDefault();

                var qry = String.Format(
    @"SELECT *
FROM {0}
WHERE {1} = {2}", type.Name, keyname, entry.PrimaryKey);

                var res = QueryDatabase(dbset, qry).Result.ToList();

                object obj = null;
                if (res.Count > 0)
                    obj = res.FirstOrDefault();

                var actualPropType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, entry.ColumnUpdated)).GetType();
                var changeValue = Convert.ChangeType(entry.OldValue, actualPropType);

                obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, entry.ColumnUpdated)).SetValue(obj, changeValue);

                var changesList = logger.ctx.RepositoryChangeLog.Where(c => c.PrimaryKey == changeGroupKeyValue && c.TableUpdated == entry.TableUpdated
                                                        && (c.RepositoryChangeLogId >= Beginning_ID && c.RepositoryChangeLogId <= Ending_ID)).ToList();

                foreach (var c in changesList)
                {
                    var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                    var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                    obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                    var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.RepositoryChangeLogId).FirstOrDefault();

                    if (logEntry != null)
                        logEntry.Operation = "Rolled Back";
                }

                UpdateContext(obj);

                logger.ctx.SaveChanges();
            }

            changeGroupKeyValue = null;
            foreach (var entry in changeLogRangeMatchingInsertList)
            {
                if (!String.IsNullOrWhiteSpace(changeGroupKeyValue) && changeGroupKeyValue == entry.PrimaryKey)
                    continue;

                var type = Type.GetType(entry.FQAType);
                var dbset = ctx.Set(type);

                changeGroupKeyValue = entry.PrimaryKey;

                var keyname = GetKeyNames(type).FirstOrDefault();
                var dbkeyname = GetDatabaseKeyNames(type).FirstOrDefault();

                var qry = String.Format(
    @"SELECT *
FROM {0}
WHERE {1} = {2}", type.Name, keyname, entry.PrimaryKey);

                var res = QueryDatabase(dbset, qry).Result.ToList();

                object obj = null;
                if (res.Count > 0)
                    obj = res.FirstOrDefault();
                // dbset.Find(new object[] { changeGroupKeyValue });
                //var changeValue = Convert.ChangeType(changeGroupKeyValue, obj.GetType().GetProperty(keyname).GetType());

                var changesList = logger.ctx.RepositoryChangeLog.Where(c => c.PrimaryKey == changeGroupKeyValue && c.TableUpdated == entry.TableUpdated
                                                        && (c.RepositoryChangeLogId >= Beginning_ID && c.RepositoryChangeLogId <= Ending_ID)).ToList();
                var keynames = GetKeyNames(type);
                var numKeysInChange = changesList
                    .Join(keynames,
                    a => Repository_Helper.GetPropertyName(type, ctx, a.ColumnUpdated),
                    k => k,
                    (a, k) => a
                    ).Count();

                if (numKeysInChange == keynames.Count() && changesList.Count >= obj.GetType().GetProperties().Count())
                {
                    foreach (var c in changesList)
                    {
                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.RepositoryChangeLogId).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }
                    dbset.Remove(obj);
                    ctx.SaveChanges();
                }
                else
                {
                    foreach (var c in changesList)
                    {
                        var propType = obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).PropertyType;
                        var convertedChangeValue = Convert.ChangeType(c.OldValue, propType);

                        obj.GetType().GetProperty(Repository_Helper.GetPropertyName(type, ctx, c.ColumnUpdated)).SetValue(obj, convertedChangeValue);

                        var logEntry = logger.ctx.RepositoryChangeLog.Where(r => r.RepositoryChangeLogId == c.RepositoryChangeLogId).FirstOrDefault();

                        if (logEntry != null)
                            logEntry.Operation = "Rolled Back";
                    }

                    UpdateContext(obj);
                }

                logger.ctx.SaveChanges();
            }

            if (changeLogRangeMatchingInsertList.ToList().Count == 0 && changeLogRangeMatchingUpdateList.ToList().Count == 0)
                return false;

            return true;
        }
        public bool RollBackChange_Made_By_Previous_Repo_ByID(long ChangeLog_Entry_ID)
        {
            CheckDisposedAndKillOnDispose();
            var entry = logger.ctx.RepositoryChangeLog.Where(q => q.RepositoryChangeLogId == ChangeLog_Entry_ID).FirstOrDefault();

            if (entry == null)
                return false;

            var type = Type.GetType(entry.TableUpdated);
            var dbset = ctx.Set(type);

            var keyname = GetKeyNames(type).FirstOrDefault();

            var qry = String.Format(
@"SELECT *
FROM {0}
WHERE {1} = {2}", type.Name, keyname, entry.PrimaryKey);

            var res = QueryDatabase(dbset, qry).Result.ToList();

            object obj = null;
            if (res.Count > 0)
                obj = res.FirstOrDefault();

            var actualPropName = Repository_Helper.GetPropertyName(type, ctx, entry.ColumnUpdated);
            var actualPropType = obj.GetType().GetProperty(actualPropName).GetType();
            var convertedChangeValue = Convert.ChangeType(entry.OldValue, actualPropType);

            obj.GetType().GetProperty(actualPropName).SetValue(obj, convertedChangeValue);

            if (keyname == actualPropName)
                return false;

            entry.Operation = "Rolled Back";

            UpdateContext(obj);

            logger.ctx.SaveChanges();

            return true;
        }

        private void CheckDisposedAndKillOnDispose()
        {
            try
            {
                ctx.Database.Exists();
            }
            catch (InvalidOperationException)
            {
                if (!KillConnectionOnDispose)
                {
                    var connfactory = new SqlConnectionFactory();
                    ConnectionToUse = connfactory.CreateConnection(ConnectionString);
                    ctx = new DbContext(new EntityConnection(Workspace, ConnectionToUse, KillConnectionOnDispose), KillConnectionOnDispose);

                    ctx.Database.Connection.Open();
                    ctx.Database.Initialize(true);

                    logger.LoadContext(ctx.Database.Connection, KillConnectionOnDispose);
                }
                else
                    throw new Exceptions.Repository_ContextDisposeException("Context was disposed, and KillConnectionOnDispose was set to " + KillConnectionOnDispose);
            }
        }

        private async Task<List<object>> QueryDatabase(DbSet dbset, string qry)
        {
            return await dbset.SqlQuery(qry).ToListAsync();
        }
        private T Clone<T>(T value)
        {
            string json = JsonConvert.SerializeObject(value);

            return JsonConvert.DeserializeObject<T>(json);
        }

        #region IDispose
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ctx.Dispose();

                    ContextChangesQueue.Clear();
                    ContextChangesQueue = null;

                    _ChangeLogQueue.Clear();
                    _ChangeLogQueue = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
