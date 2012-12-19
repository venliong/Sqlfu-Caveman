using System;
using CavemanTools.Logging;
using SqlFu.Migrations;

namespace SqlFu.Caveman
{
    public abstract class AbstractCavemanRepository
    {
        protected Func<IAccessDb> DbFactory;

        protected abstract string TableName { get; }
        protected abstract string StorageName { get; }

        public AbstractCavemanRepository(Func<IAccessDb> dbFactory)
        {
            DbFactory = dbFactory;           
        }


        public void EnsureStorage()
        {
            LogHelper.DefaultLogger.Debug("[{0}] Checking if storage is initiated...", StorageName);
           
            if (!IsStoreCreated())
            {
                LogHelper.DefaultLogger.Debug("[{0}] Initiating storage...",StorageName);
                InitStorage();
                LogHelper.DefaultLogger.Debug("[{0}] Storage created",StorageName);
            }
        }

        void InitStorage()
        {
            DatabaseMigration.ConfigureFor(DbFactory())
               .SearchAssembly(this.GetType().Assembly)
               .PerformAutomaticMigrations(this.MigrationSchemaName);
                                 
        }

        public bool IsStoreCreated()
        {
            using (var _db = DbFactory())
            {
                return _db.DatabaseTools.TableExists(TableName);
            }            
        }


        protected abstract string MigrationSchemaName { get; }
        /// <summary>
        /// Deletes underlying table
        /// </summary>
        public void DestroyStorage()
        {
            using (var _db = DbFactory())
            {
                _db.DatabaseTools.DropTable(TableName);
                DatabaseMigration.ConfigureFor(_db)
                    .SearchAssembly(GetType().Assembly)
                    .BuildAutomaticMigrator().Untrack(MigrationSchemaName);
            }
            LogHelper.DefaultLogger.Info("[{0}] Storage destroyed.", StorageName);
        }
    }
}