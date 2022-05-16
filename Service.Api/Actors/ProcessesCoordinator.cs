using Akka.Actor;
using Akka.Routing;
using Service.Api.Models;

namespace Service.Api.Actors
{
    public class ProcessesCoordinator : BaseActor
    {
        protected IActorRef _messageBroadcaster;

        public ProcessesCoordinator()
        {
            Ready();
        }

        protected void Ready()
        {
            Receive<RawSendMessage>(message =>
            {
                var sourceActor = Context.Child(message.SourcePhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.SourcePhone), message.SourcePhone));

                var destinationActor = Context.Child(message.DestinationPhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.DestinationPhone), message.DestinationPhone));

                sourceActor.Forward(new User.SendMessage(destinationActor, message.Message));
                Sender.Tell(message.Message.Id);
            });

            Receive<RawEditMessage>(message =>
            {
                var sourceActor = Context.Child(message.SourcePhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.SourcePhone), message.SourcePhone));

                var destinationActor = Context.Child(message.DestinationPhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.DestinationPhone), message.DestinationPhone));

                sourceActor.Forward(new User.EditMessage(message.MessageId, destinationActor, message.Message));
            });

            ReceiveAsync<RawReadNewMessages>(async message =>
            {
                var sourceActor = Context.Child(message.SourcePhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.SourcePhone), message.SourcePhone));
               
                var result = await sourceActor.Ask<object>(new User.ReadNewMessages(message.SourcePhone));
                Sender.Tell(result);
            });

            ReceiveAsync<RawReadAllMessages>(async message =>
            {
                var sourceActor = Context.Child(message.SourcePhone)
                            .GetOrElse(() => Context.ActorOf(_sp.Props<User>(message.SourcePhone), message.SourcePhone));

                var result = await sourceActor.Ask<object>(new User.ReadAllMessages(message.SourcePhone));
                Sender.Tell(result);
            });
        }

        public sealed class RawSendMessage
        {
            public RawSendMessage(string sourcePhone, Message message, string destinationPhone)
            {
                SourcePhone = sourcePhone;
                Message = message;
                DestinationPhone = destinationPhone;
            }

            public string SourcePhone { get; }
            public Message Message { get; }
            public string DestinationPhone { get; }
        }

        public sealed class RawEditMessage
        {
            public RawEditMessage(Guid messageId, string sourcePhone, string message, string destinationPhone)
            {
                SourcePhone = sourcePhone;
                DestinationPhone = destinationPhone;
                MessageId = messageId;
                Message = message;
            }

            public string SourcePhone { get; }
            public string Message { get; }
            public string DestinationPhone { get; }
            public Guid MessageId { get; }
        }
     
        public sealed class RawReadNewMessages
        {
            public RawReadNewMessages(string sourcePhone)
            {
                SourcePhone = sourcePhone;
            }

            public string SourcePhone { get; }
        }

        public sealed class RawReadAllMessages
        {
            public RawReadAllMessages(string sourcePhone)
            {
                SourcePhone = sourcePhone;
            }

            public string SourcePhone { get; }
        }
    }
}
