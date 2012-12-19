using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using CavemanTools.Infrastructure.MessagesBus;
using CavemanTools.Logging;
using SqlFu.DDL;
using SqlFu.Migrations;

namespace SqlFu.Caveman.MessageBus
{
   [Migration("1.0.0",SchemaName = MessageBusSqlStorage.SchemaName)]
    public class BusStorageSetup:AbstractMigrationTask
   {
       public const string TableName = "MessageBusStore";
       /// <summary>
       /// Task is executed automatically in a transaction
       /// </summary>
       /// <param name="db"/>
       public override void Execute(IAccessDb db)
       {
           var store = db.DatabaseTools.GetCreateTableBuilder(TableName, IfTableExists.DropIt);
           store.Columns
                .Add("Id", DbType.Int64, isNullable: false, autoIncrement: true).AsPrimaryKey()
                .Add("MessageId", DbType.Guid, isNullable: false)
                .Add("Body", DbType.Binary)
                .Add("CommittedAt", DbType.DateTime)
                .Add("State", DbType.Int16, isNullable: false)
                .Add("Hash", DbType.AnsiStringFixedLength, "40", false).AsUnique("uc_message")
                .Add("Failures", DbType.Int16, isNullable: false, defaultValue: "0");
                ;

           store.ExecuteDDL();
       }
   }

    public class MessageBusSqlStorage:AbstractCavemanRepository,IStoreMessageBusState
    {
        public const string SchemaName = "SqlFuMessageBus";
        public MessageBusSqlStorage(Func<IAccessDb> db) : base(db)
        {
        }

        protected override string TableName
        {
            get { return BusStorageSetup.TableName; }
        }

        protected override string StorageName
        {
            get { return "MesageBusStore"; }
        }


        protected override string MigrationSchemaName
        {
            get { return SchemaName; }
        }


        public void StoreMessageInProgress(IMessage cmd)
        {
            cmd.MustNotBeNull();
            var commit = new MessageCommit(cmd);
            commit.State = 1;
            using (var _db = DbFactory())
            {
                try
                {
                    _db.Insert(TableName, commit);
                }
                catch (DbException ex)
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
          
        }

       

        public void MarkMessageFailed(Guid id)
        {
            using (var db = DbFactory())
            {
                db.ExecuteCommand("update " + TableName + " set Failures=Failures+1 where MessageId=@0 and State=1",id);
            }
        }

        public void MarkMessageCompleted(Guid id)
        {
            using (var _db = DbFactory())
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
            
        }

        public IEnumerable<IMessage> GetUncompletedMessages(int failCount=3)
        {
            using (var _db = DbFactory())
            {
                return _db.Fetch<MessageCommit>("select Body from " + TableName + " where Messageid in (select MessageId from " + TableName +
                                     " group by MessageId having sum(State)=1) and Failures <@0",failCount+1).Select(m => m.ToMessage());
            }
            
        }

        public void EmptyStore()
        {
            if (IsStoreCreated())
            {
                using (var _db = DbFactory())
                {
                    _db.ExecuteCommand("truncate table " + TableName);
                }                
                LogHelper.DefaultLogger.Debug("[MessageBusStore] Store was emptied.");
            }
            
        }

        public void Cleanup()
        {
            using (var _db = DbFactory())
            {
                _db.ExecuteCommand("delete from " + TableName + " where MessageId in (select MessageId from " + TableName +
                               " group by MessageId having sum(State)=0)");
            }
            LogHelper.DefaultLogger.Info("[MessageBusStore] Storage cleaned.");
        }
    }
}