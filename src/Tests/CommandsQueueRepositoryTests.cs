using System.Linq;
using CavemanTools.Infrastructure;
using SqlFu.Caveman;
using Xunit;
using System;
using System.Diagnostics;

namespace Tests
{
    public class CommandsQueueRepositoryTests:IDisposable
    {
        private Stopwatch _t = new Stopwatch();
        private CommandsQueueRepository _repo;

        public CommandsQueueRepositoryTests()
        {
            _repo = new CommandsQueueRepository(Setup.GetDb());
            _repo.EnsureStorage();
        }

        [Fact]
        public void save_and_get()
        {
            var item = CreateQueueItem();
            _repo.Save(item);
            var all = _repo.GetItems(DateTime.UtcNow);
            Assert.Equal(1,all.Count());
            Assert.Equal(2,all.First().Command.Cast<MyCommand>().Test.Id);
        }

        private static QueueItem CreateQueueItem()
        {
            var item = new QueueItem(new MyCommand() {Test = new MyTest() {Id = 2}});
            item.ExecuteAt = DateTime.UtcNow;
            return item;
        }

        [Fact]
        public void mark_as_complete()
        {
            var item = CreateQueueItem();
            _repo.Save(item);
            _repo.MarkItemAsExecuted(item.Id);
            Assert.Empty(_repo.GetItems(DateTime.UtcNow));
        }


        [Fact]
        public void item_in_the_future_shouldnt_be_returned()
        {
            var item = CreateQueueItem();
            item.ExecuteAt = DateTime.UtcNow.AddDays(1);
            _repo.Save(item);
            Assert.Empty(_repo.GetItems(DateTime.UtcNow));
        }

        [Fact]
        public void mark_as_fail_ignore_after_one_fail()
        {
            var item = CreateQueueItem();
            _repo.Save(item);
            _repo.FailureCountToIgnore = 1;
            _repo.MarkItemAsFailed(item.Id);
            Assert.Equal(1,_repo.GetItems(DateTime.UtcNow).Count());
            _repo.MarkItemAsFailed(item.Id);
            Assert.Empty(_repo.GetItems(DateTime.UtcNow));
            
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