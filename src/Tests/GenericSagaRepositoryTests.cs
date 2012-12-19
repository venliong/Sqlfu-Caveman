using CavemanTools.Infrastructure;
using CavemanTools.Infrastructure.MessagesBus.Saga;
using SqlFu.Caveman;
using SqlFu.Caveman.MessageBus;
using Xunit;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Tests
{
    public class MySagaState:ISagaState
    {
        /// <summary>
        /// When using the generic saga repository, this must have a value which identifies the correct saga
        /// </summary>
        public string SagaCorrelationId { get; set; }
        public Guid Id { get; set; }
        public bool IsCompleted { get; set; }
        public int Value { get; set; }
    }
    
    public class GenericSagaRepositoryTests:IDisposable
    {
        private Stopwatch _t = new Stopwatch();
        private GenericSagaSqlRepository _repo;

        public GenericSagaRepositoryTests()
        {
            _repo = new GenericSagaSqlRepository(Setup.GetDb(),ActivatorContainer.Instance);
            _repo.EnsureStorage();
        }

        [Fact]
        public void no_saga_correlation_id_should_throw()
        {
            Assert.Throws<InvalidOperationException>(() => _repo.Save(new MySagaState()));
        }

        [Fact]
        public void save_load_saga()
        {
            var saga = new MySagaState() {Id = Guid.NewGuid(), SagaCorrelationId = "2",IsCompleted = true};
            _repo.Save(saga); 
            Write(saga.GetType().GetFullTypeName());
            var saga1 = _repo.GetSaga("2", typeof (MySagaState));
            Assert.Equal(saga.Id,saga1.Id);
            Assert.True(saga1.IsCompleted);
        }


        [Fact]
        public void saga_notfound_returns_new_saga_instance()
        {
            Assert.NotNull(_repo.GetSaga("45", typeof (MySagaState)));
        }

        [Fact]
        public void new_saga_instance_has_id_set()
        {
            var saga=_repo.GetSaga("45", typeof (MySagaState));
            Assert.NotEqual(Guid.Empty,saga.Id);
        }


        [Fact]
        public void new_saga_instance_ha_correlationId_set()
        {
            var saga = _repo.GetSaga("45", typeof(MySagaState));
            Assert.Equal("45",saga.SagaCorrelationId);
        }

        [Fact]
        public void if_saga_exists_update_it()
        {
            var saga = _repo.GetSaga("25", typeof (MySagaState)) as MySagaState;
            _repo.Save(saga);
            saga.Value = 56;
            _repo.Save(saga);
            var saga1 = _repo.GetSaga("25", typeof (MySagaState)) as MySagaState;
            Assert.Equal(56,saga1.Value);            
        }

        [Fact]
        public void count_sagas()
        {
            var saga = _repo.GetSaga("25", typeof(MySagaState)) as MySagaState;
            _repo.Save(saga);
            saga.IsCompleted = true;
            _repo.Save(saga);
            Assert.Equal(1,_repo.CountAllSagas());
            Assert.Equal(0,_repo.CountAllSagas(onlyActive:true));
        }

        [Fact]
        public void only_completed_are_cleanedup()
        {
            var saga = _repo.GetSaga("25", typeof(MySagaState)) as MySagaState;
            _repo.Save(saga);
            _repo.CleanUp(DateTime.UtcNow);
            Assert.Equal(1,_repo.CountAllSagas());

            saga.IsCompleted = true;
            _repo.Save(saga);
            _repo.CleanUp(DateTime.UtcNow);
            Assert.Equal(0,_repo.CountAllSagas());
        }

        protected void Write(string format, params object[] param)
        {
            Console.WriteLine(format, param);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _repo.DestroyStorage();            
        }
    }
}