using System;
using CavemanTools.Logging;
using SqlFu;

namespace Tests
{
    public class Setup
    {
        public const string Connex = "Data Source=.;Initial Catalog=tempdb;Integrated Security=True";
 
        static Setup()
        {
            LogHelper.Register(new ConsoleLogger(), "dev");
        }

        public static Func<IAccessDb> GetDb()
        {
            return () =>
                {
                    LogHelper.DefaultLogger.Debug("new DbAccess instance");
                    var db = new DbAccess(Connex, DbEngine.SqlServer);
                    db.OnCommand = cmd => LogHelper.DefaultLogger.Debug(cmd.FormatCommand());
                    db.OnOpenConnection = cmd => LogHelper.DefaultLogger.Debug("Open");
                    db.OnCloseConnection = cmd => LogHelper.DefaultLogger.Debug("Close");
                    db.OnException =
                        (s, ex) => LogHelper.DefaultLogger.Debug("Exception: {0} \n\r {1}", s.ExecutedSql, ex.ToString());
                    return db;
                };
        }
    }
}