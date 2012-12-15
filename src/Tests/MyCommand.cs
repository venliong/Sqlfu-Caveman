using CavemanTools.Infrastructure.MessagesBus;

namespace Tests
{
    public class MyCommand:AbstractCommand
    {
        public ITest Test { get; set; }
    }
}