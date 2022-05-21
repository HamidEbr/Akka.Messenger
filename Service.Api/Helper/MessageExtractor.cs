using Akka.Cluster.Sharding;

namespace Service.Api.Helper
{
    public sealed class ShardEnvelope
    {
        public readonly string EntityId;
        public readonly object Payload;

        public ShardEnvelope(string entityId, object payload)
        {
            EntityId = entityId;
            Payload = payload;
        }
    }

    public sealed class MessageExtractor : HashCodeMessageExtractor
    {
        public MessageExtractor(int maxNumberOfShards) : base(maxNumberOfShards)
        {
        }

        public override string EntityId(object message)
        {
            return message switch
            {
                ShardRegion.StartEntity start => start.EntityId,
                ShardEnvelope e => e.EntityId,
                _ => string.Empty,
            };
        }

        public override object EntityMessage(object message)
        {
            return message switch
            {
                ShardEnvelope e => e.Payload,
                _ => message,
            };
        }
    }
}
