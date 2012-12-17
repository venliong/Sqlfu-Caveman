using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CavemanTools.Infrastructure;
using SqlFu.DDL;

namespace SqlFu.Caveman
{
    public class CommandsQueueRepository:AbstractCavemanRepository,ISaveQueueState
    {
        public CommandsQueueRepository(IAccessDb db) : base(db)
        {
        }

        protected override string TableName
        {
            get { return "CavemanCommandsQueue"; }
        }

        protected override string StorageName
        {
            get { return "CommandsQueueRepository"; }
        }

        protected override void InitStorage()
        {
            var table = _db.DatabaseTools.GetCreateTableBuilder(TableName, IfTableExists.DropIt);
            table.Columns
                .Add("Id", DbType.Guid, isNullable: false).AsPrimaryKey()
                .Add("Body",DbType.Binary,isNullable:false)
                .Add("ShouldRunAt",DbType.DateTime,isNullable:false)
                .Add("CompletedAt",DbType.DateTime)
                ;
            table.ExecuteDDL();
        }

        public void Save(QueueItem item)
        {
            item.MustNotBeNull();
            _db.Insert(TableName, new {Id = item.Id, Body = item.Serialize(), ShouldRunAt = item.ExecuteAt}, false);
        }

        public void MarkItemAsExecuted(Guid id)
        {
            _db.Update(TableName, new {Id = id, CompletedAt = DateTime.UtcNow});
        }


        /// <summary>
        /// Gets items with execution date older than date
        /// </summary>
        /// <param name="date"/>
        /// <returns/>
        public IEnumerable<QueueItem> GetItems(DateTime date,int maxItems=50)
        {
            var res=_db.PagedQuery<byte[]>(0,maxItems,@"select Body from " + TableName + " where CompletedAt is null and ShouldRunAt<=@0", date);
             return res.Items.Select(d => (QueueItem) d.Deserialize());
        }

        /// <summary>
        /// Delete all completed commands before date
        /// </summary>
        /// <param name="before"></param>
        public void Cleanup(DateTime before)
        {
            _db.ExecuteCommand("delete from " + TableName + " where CompletedAt<@0", before);
        }
    }
}