using System.Linq;
using CavemanTools.Infrastructure.MessagesBus;
using CavemanTools.Logging;
using SqlFu;
using SqlFu.Caveman;
using SqlFu.Caveman.MessageBus;
using Xunit;
using System;
using System.Diagnostics;

namespace Tests
{
    public interface ITest
    {
        int Id { get; }
    }

    class MyTest:ITest
    {
        public int Id { get; set; }
    }

    public class MessageBusSqlStorageTests:IDisposable
    {
        private Stopwatch _t = new Stopwatch();
        private MessageBusSqlStorage _store;
        
        public MessageBusSqlStorageTests()
        {
           _store = new MessageBusSqlStorage(Setup.GetDb());
            _store.EnsureStorage();
        }

        [Fact]
        public void init_storage()
        {
            Assert.True(_store.IsStoreCreated());
        }

        [Fact]
        public void insert_uncompleted_message()
        {
            var cmd = new MyCommand()
                          {
                              Test = new MyTest()
                                         {
                                             Id = 34
                                         }
                          };
            _store.StoreMessageInProgress(cmd);
            var all = _store.GetUncompletedMessages().ToArray();
            Assert.Equal(1,all.Count());
            Assert.Equal(34,all.First().Cast<MyCommand>().Test.Id);
        }

        [Fact]
        public void complete_message()
        {
            var cmd = new MyCommand()
            {
                Test = new MyTest()
                {
                    Id = 34
                }
            };
            
            _store.StoreMessageInProgress(cmd);
            _store.MarkMessageCompleted(cmd.Id);

            cmd = new MyCommand()
            {
                Test = new MyTest()
                {
                    Id = 45
                }
            };

            _store.StoreMessageInProgress(cmd);
            _store.MarkMessageCompleted(cmd.Id);
            Assert.DoesNotThrow(() => _store.MarkMessageCompleted(cmd.Id));
            
            var all = _store.GetUncompletedMessages().ToArray();
            Assert.Equal(0, all.Count());
        }

        [Fact]
        public void mark_as_failed()
        {
            var cmd = new MyCommand();
            _store.StoreMessageInProgress(cmd);
            Assert.DoesNotThrow(()=>_store.MarkMessageFailed(cmd.Id));
            Assert.Empty(_store.GetUncompletedMessages(0));
        }

        [Fact]
        public void duplicate_message_throws()
        {
            var cmd = new MyCommand()
            {
                Test = new MyTest()
                {
                    Id = 34
                }
            };
            _store.StoreMessageInProgress(cmd);
            Assert.Throws<DuplicateMessageException>(() =>
                                                         {
                                                             _store.StoreMessageInProgress(cmd);
                                                         });
            
        }

        private void Write(string format, params object[] param)
        {
            Console.WriteLine(format, param);
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _store.DestroyStorage();
        }
    }
}