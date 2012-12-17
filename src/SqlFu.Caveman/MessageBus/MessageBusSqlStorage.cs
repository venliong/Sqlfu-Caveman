using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using CavemanTools.Infrastructure.MessagesBus;
using CavemanTools.Logging;
using SqlFu.DDL;

namespace SqlFu.Caveman.MessageBus
{
    public class MessageBusSqlStorage:AbstractCavemanRepository,IStoreMessageBusState
    {
        public MessageBusSqlStorage(IAccessDb db) : base(db)
        {
        }

        protected override string TableName
        {
            get { return "MessageBusStore"; }
        }

        protected override string StorageName
        {
            get { return "MesageBusStore"; }
        }


        protected override void InitStorage()
        {
            var store = _db.DatabaseTools.GetCreateTableBuilder(TableName,IfTableExists.DropIt);
            store.Columns
                 .Add("Id", DbType.Int64, isNullable: false, autoIncrement: true).AsPrimaryKey()
                 .Add("MessageId", DbType.Guid, isNullable: false)
                 .Add("Body", DbType.Binary)
                 .Add("CommittedAt", DbType.DateTime)
                 .Add("State", DbType.Int16, isNullable: false)
                 .Add("Hash", DbType.AnsiStringFixedLength, "40",false).AsUnique("uc_message");
            store.ExecuteDDL();
        }

        

        public void StoreMessageInProgress(IMessage cmd)
        {
            cmd.MustNotBeNull();
            var commit = new MessageCommit(cmd);
            commit.State = 1;
            try
            {
                _db.Insert(TableName,commit);
            }
            catch(DbException ex)
            {
                if (ex.Message.Contains("uc_message"))
                {
                    throw new DuplicateMessageException();
                }
                else
                {
                    throw;
                }
            }
        }

        public void StoreMessageCompleted(Guid id)
        {
            try
            {
                _db.Insert(TableName, new { MessageId = id, State = -1, CommittedAt = DateTime.UtcNow, Hash = id });
            }
            catch (DbException ex)
            {
                //if message has already been completed ignore it
                if (!ex.Message.Contains("uc_message"))
                {
                    throw;
                }
            }
            
            
        }

        public IEnumerable<IMessage> GetUncompletedMessages()
        {
            return _db.Fetch<MessageCommit>("select Body from " + TableName + " where Messageid in (select MessageId from " + TableName +
                                     " group by MessageId having sum(State)=1)").Select(m=>m.ToMessage());
        }

        public void EmptyStore()
        {
            if (IsStoreCreated())
            {
                _db.ExecuteCommand("truncate table " + TableName);
                LogHelper.DefaultLogger.Debug("[MessageBusStore] Store was emptied.");
            }
            
        }

        public void Cleanup()
        {
            _db.ExecuteCommand("delete from " + TableName + " where MessageId in (select MessageId from " + TableName +
                               " group by MessageId having sum(State)=0)");
            LogHelper.DefaultLogger.Info("[MessageBusStore] Storage cleaned.");
        }
    }
}