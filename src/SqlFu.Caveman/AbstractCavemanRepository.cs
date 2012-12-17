using CavemanTools.Logging;

namespace SqlFu.Caveman
{
    public abstract class AbstractCavemanRepository
    {
        protected IAccessDb _db;

        protected abstract string TableName { get; }
        protected abstract string StorageName { get; }

        public AbstractCavemanRepository(IAccessDb db)
        {
            _db = db;
            EnsureStorage();
        }

        
        protected void EnsureStorage()
        {
            LogHelper.DefaultLogger.Debug("[{0}] Checking if storage is initiated...", StorageName);
            _db.OnCommand = cmd => LogHelper.DefaultLogger.Debug(cmd.FormatCommand());
            _db.OnException =
                (s, ex) => LogHelper.DefaultLogger.Debug("Exception: {0} \n\r {1}", s.ExecutedSql, ex.ToString());

            if (!IsStoreCreated())
            {
                LogHelper.DefaultLogger.Debug("[MessageBusStore] Initiating storage...");
                InitStorage();
                LogHelper.DefaultLogger.Debug("[MessageBusStore] Storage created");
            }
        }

        protected abstract void InitStorage();

        public bool IsStoreCreated()
        {
            return _db.DatabaseTools.TableExists(TableName);
        }

        /// <summary>
        /// Deletes underlying table
        /// </summary>
        public void DestroyStorage()
        {
            _db.DatabaseTools.DropTable(TableName);
            LogHelper.DefaultLogger.Info("[{0}] Storage destroyed.", StorageName);
        }
    }
}