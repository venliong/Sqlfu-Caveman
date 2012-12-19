using System;
using System.Data;
using CavemanTools.Infrastructure;
using CavemanTools.Infrastructure.MessagesBus.Saga;
using SqlFu.DDL;
using System.Reflection;
using SqlFu.Migrations;

namespace SqlFu.Caveman.MessageBus
{
    [Migration("1.0.0",SchemaName = GenericSagaSqlRepository.MigrationSchema)]
    public class SagaRepositorySetup:AbstractMigrationTask
    {
        public const string TableName = "CavemanSagas";
        /// <summary>
        /// Task is executed automatically in a transaction
        /// </summary>
        /// <param name="db"/>
        public override void Execute(IAccessDb _db)
        {
            var store = _db.DatabaseTools.GetCreateTableBuilder(TableName, IfTableExists.DropIt);
                store.Columns
                     .Add("Id", DbType.Guid, isNullable: false).AsPrimaryKey()
                     .Add("Body", DbType.Binary, isNullable: false)
                     .Add("CorrelationId", DbType.String, "250", false)
                     .Add("SagaType", DbType.String, "250", false)
                     .Add("CompletedAt", DbType.DateTime)
                    ;
                store.Indexes.AddIndexOn("CorrelationId,SagaType", true, "uix_saga");
                store.ExecuteDDL();
            
        }
    }
    
    public class GenericSagaSqlRepository:AbstractCavemanRepository,IGenericSagaRepository
    {
        public const string MigrationSchema = "SqlFuSagaRepository";
        public GenericSagaSqlRepository(Func<IAccessDb> db,IResolveDependencies resolver) : base(db)
         {
             resolver.MustNotBeNull();
             _resolver = resolver;
         }

        private IResolveDependencies _resolver;
        


 
        protected override string TableName
        {
            get { return SagaRepositorySetup.TableName; }
        }

        protected override string StorageName
        {
            get { return "CavemanSagaRepository"; }
        }

        protected override string MigrationSchemaName
        {
            get { return MigrationSchema; }
        }


        public void Save(ISagaState state)
        {
            if (state.SagaCorrelationId.IsNullOrEmpty()) throw new InvalidOperationException("SagaCorrelationId must have a value");
           using (var _db = DbFactory())
           {
              var oldExists = _db.GetValue<bool>("select count(id) from " + TableName + " where Id=@0", state.Id);
            if (!oldExists)
            {
                _db.Insert(TableName,new {Id = state.Id, Body = state.Serialize(),CorrelationId=state.SagaCorrelationId,SagaType=state.GetType().GetFullTypeName()},false);
            }
            else
            {
                DateTime? completedAt = state.IsCompleted ? DateTime.UtcNow : new DateTime?();
                _db.Update(TableName, new {state.Id, Body = state.Serialize(), CompletedAt = completedAt});
            }  
           }
           
        }

        public ISagaState GetSaga(string correlationId, Type sagaType)
        {
            correlationId.MustNotBeEmpty();
            using (var _db = DbFactory())
            {
                var data =
                _db.FirstOrDefault<byte[]>(@"select Body from " + TableName + " where CorrelationId=@0 and SagaType=@1",
                                            correlationId, sagaType.GetFullTypeName());
                ISagaState result;
                if (data == null)
                {
                    result = (ISagaState)_resolver.Resolve(sagaType);
                    result.Id = Guid.NewGuid();
                    result.SagaCorrelationId = correlationId;
                }
                else
                {
                    result = Serializer.Deserialize(data) as ISagaState;
                }
                return result;
            }

        }

        public void CleanUp(DateTime before)
        {
            using (var _db = DbFactory())
            {
                _db.ExecuteCommand("delete from " + TableName + " where CompletedAt<@0", before);
            }
        }

        public long CountAllSagas(bool onlyActive=false)
        {
            var sql = "select count(*) from " + TableName;
            if (onlyActive)
            {
                sql = sql + " where CompletedAt is null";
            }
            using (var _db = DbFactory())
             {
                 return _db.GetValue<long>(sql);
             }
           
            
        }

        
    }

}