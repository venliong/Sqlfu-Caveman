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

        public static DbAccess GetDb()
        {
            return new DbAccess(Connex, DbEngine.SqlServer);
        }
    }
}