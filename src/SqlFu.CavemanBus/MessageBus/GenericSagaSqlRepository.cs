using System;
using System.Data;
using CavemanTools.Infrastructure;
using CavemanTools.Infrastructure.MessagesBus.Saga;
using SqlFu.DDL;

namespace SqlFu.Caveman.MessageBus
{
    public class GenericSagaSqlRepository:AbstractCavemanRepository,IGenericSagaRepository
    {
         public GenericSagaSqlRepository(IAccessDb db,IResolveDependencies resolver) : base(db)
         {
             resolver.MustNotBeNull();
             _resolver = resolver;
         }

        private IResolveDependencies _resolver;
        

        protected override void InitStorage()
         {
             var store = _db.DatabaseTools.GetCreateTableBuilder(TableName,IfTableExists.DropIt);
             store.Columns
                  .Add("Id", DbType.Guid, isNullable: false).AsPrimaryKey()
                  .Add("Body", DbType.Binary, isNullable: false)
                  .Add("CorrelationId", DbType.String, "250", false)
                  .Add("SagaType",DbType.String,"250",false)
                 ;
             store.Indexes.AddIndexOn("CorrelationId,SagaType",true);
             store.ExecuteDDL();
         }

 
        protected override string TableName
        {
            get { return "CavemanSagas"; }
        }

        protected override string StorageName
        {
            get { return "CavemanSagaRepository"; }
        }


        public void Save(ISagaState state)
        {
            if (state.SagaCorrelationId.IsNullOrEmpty()) throw new InvalidOperationException("SagaCorrelationId must have a value");

            _db.Insert(TableName,new {Id = state.Id, Body = state.Serialize(),CorrelationId=state.SagaCorrelationId,SagaType=state.GetType().AssemblyQualifiedName},false);
        }

        public ISagaState GetSaga(string correlationId, Type sagaType)
        {
            correlationId.MustNotBeEmpty();
            var data =
                _db.FirstOrDefault<byte[]>(@"select Body from " + TableName + " where CorrelationId=@0 and SagaType=@1",
                                            correlationId, sagaType.AssemblyQualifiedName);
            ISagaState result;
            if (data == null)
            {
                result= (ISagaState) _resolver.Resolve(sagaType);
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
}