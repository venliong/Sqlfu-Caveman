using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CavemanTools.Infrastructure;
using SqlFu.DDL;
using SqlFu.Migrations;

namespace SqlFu.Caveman
{
    [Migration("1.0.0",SchemaName = CommandsQueueRepository.SchemaName)]
    public class CommandsQueueSetup:AbstractMigrationTask
    {
        internal const string TableName = "CavemanCommandsQueue";
        /// <summary>
        /// Task is executed automatically in a transaction
        /// </summary>
        /// <param name="db"/>
        public override void Execute(IAccessDb db)
        {
            var table = db.DatabaseTools.GetCreateTableBuilder(TableName, IfTableExists.DropIt);
            table.Columns
                .Add("Id", DbType.Guid, isNullable: false).AsPrimaryKey()
                .Add("Body", DbType.Binary, isNullable: false)
                .Add("QueuedAt", DbType.DateTime, isNullable: false)
                .Add("ShouldRunAt", DbType.DateTime, isNullable: false)
                .Add("CompletedAt", DbType.DateTime)
                .Add("Failures",DbType.Int16,isNullable:false,defaultValue:"0")
                ;
            table.ExecuteDDL();
        }
    }
    
    public class CommandsQueueRepository:AbstractCavemanRepository,ISaveQueueState
    {
        public const string SchemaName = "SqlFuCavemanQueueStorage";
        public CommandsQueueRepository(Func<IAccessDb> db) : base(db)
        {
        }

        protected override string TableName
        {
            get { return CommandsQueueSetup.TableName; }
        }

        protected override string StorageName
        {
            get { return "CommandsQueueRepository"; }
        }

       

        protected override string MigrationSchemaName
        {
            get { return SchemaName; }
        }

        public void Save(QueueItem item)
        {
            item.MustNotBeNull();
            using (var _db = DbFactory())
            {
                _db.Insert(TableName, new {Id = item.Id, Body = item.Serialize(), QueuedAt=DateTime.UtcNow, ShouldRunAt = item.ExecuteAt}, false);
            }
        }

        public void MarkItemAsExecuted(Guid id)
        {
            using (var _db = DbFactory())
            {
                _db.Update(TableName, new {Id = id, CompletedAt = DateTime.UtcNow});
            }
        }

        public void MarkItemAsFailed(Guid id)
        {
           using (var db = DbFactory())
           {
               db.ExecuteCommand("update " + TableName + " set Failures=Failures+1 where Id=@0", id);
           }
        }


        /// <summary>
        /// Gets items with execution date older than date
        /// </summary>
        /// <param name="date"/>
        /// <returns/>
        public IEnumerable<QueueItem> GetItems(DateTime date,int maxItems=50)
        {
            using (var _db = DbFactory())
            {
                var res = _db.PagedQuery<byte[]>(0, maxItems, @"select Body from " + TableName + " where CompletedAt is null and ShouldRunAt<=@0 and Failures<@1", date,FailureCountToIgnore+1);
                return res.Items.Select(d => (QueueItem)d.Deserialize());
            }
            
        }

        /// <summary>
        /// Items which failed more than this value will be ignored by the repository
        /// </summary>
        public int FailureCountToIgnore { get; set; }

        /// <summary>
        /// Delete all completed commands before date
        /// </summary>
        /// <param name="before"></param>
        public void Cleanup(DateTime before)
        {
            using (var _db = DbFactory())
            {
                _db.ExecuteCommand("delete from " + TableName + " where CompletedAt<@0", before);
            }
        }
    }
}