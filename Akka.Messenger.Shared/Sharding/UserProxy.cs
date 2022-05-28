using Akka.Actor;
using Akka.Event;

namespace Akka.Messenger.Shared.Sharding
{
    public class UserProxy : ReceiveActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly IActorRef _userActionsShardRegion;

        public UserProxy(IActorRef userActionsShardRegion)
        {
            _userActionsShardRegion = userActionsShardRegion;

            ReceiveAsync<ShardEnvelope>(async m =>
            {
                _log.Info("UserProxy received UserAction: {0}", m);
                var result = await _userActionsShardRegion.Ask(m);
                Sender.Tell(result);
            }, m => m.Payload is UserEntity.SendSmsMessage || 
                m.Payload is UserEntity.ReadNewSmsesMessage || 
                m.Payload is UserEntity.ReadAllSmsesMessage || 
                m.Payload is UserEntity.EditSmsMessage);

            ReceiveAnyAsync(async o =>
            {
                _userActionsShardRegion.Tell(o);
                //_log.Warning("Unknown message: {0}", o);
            });
        }
    }
}