using Akka.Actor;

namespace Service.Api.Services
{
    public interface IMessageSessionHandler
    {
        IActorRef ShardRegion { get; }
        void Tell(object msg);
        Task<T> Ask<T>(object msg);
    }
}
