using System;
using System.Linq;
using System.Text;
using CavemanTools.Infrastructure.MessagesBus;
using Newtonsoft.Json;

namespace SqlFu.Caveman.MessageBus
{
    public class MessageCommit
    {
        public MessageCommit()
        {

        }

        public MessageCommit(IMessage msg)
        {
            Body = msg.Serialize();
            Hash = GenerateHash(msg);
            MessageId = msg.Id;
            CommittedAt = DateTime.UtcNow;           
        }

       

        public static string GenerateHash(IMessage msg)
        {
            msg.MustNotBeNull();
            var sb = new StringBuilder();
            sb.Append(msg.SourceId.Value);
            msg.ToDictionary()
                .Where(d => d.Key != "Id" && d.Key != "SourceId" && d.Key != "TimeStamp")
                .Select(kv => JsonConvert.SerializeObject(kv.Value))
                .ForEach(s => sb.Append(s));
            return sb.ToString().Sha1();
        }

        public long Id { get; set; }
        public Guid MessageId { get; set; }
        public byte[] Body { get; set; }
        public DateTime CommittedAt { get; set; }
        public int State { get; set; }
        public string Hash { get; set; }

        public IMessage ToMessage()
        {
            return (IMessage) Body.Deserialize();
        }
    }
}