using Akka.Actor;
using Akka.Cluster.Sharding;
using Service.Api.Actors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            switch (message)
            {
                case ShardRegion.StartEntity start: return start.EntityId;
                case ShardEnvelope e: return e.EntityId;
            }

            return null;
        }

        //public override object EntityMessage(object message)
        //{
        //    switch (message)
        //    {
        //        case ShardEnvelope e: return ProcessUserMessages(e.Payload);
        //        default:
        //            return message;
        //    }
        //}

        //    public override string ShardId(object message)
        //    {
        //        switch (message)
        //        {
        //            case ShardEnvelope e: return e.EntityId;
        //        }
        //        return base.ShardId(message);
        //    }

        //    private object ProcessUserMessages(object payload)
        //    {
        //        switch (payload)
        //        {
        //            case ProcessesCoordinatorActor.RawSendMessage m: return m;
        //            default: return payload;
        //        }
        //        throw new NotImplementedException();
        //    }
    }
}
