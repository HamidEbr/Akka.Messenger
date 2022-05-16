using Akka.Actor;
using Service.Api.Models;

namespace Service.Api.Actors
{
    public class User : BaseActor
    {
        private Dictionary<Guid, SendMessage> _sentItems;
        private Dictionary<Guid, ReciveMessage> _rcvdItems;
        
        public string Phone { get; }

        public User(string userPhone)
        {
            Phone = userPhone;
            _sentItems = new Dictionary<Guid, SendMessage>();
            _rcvdItems = new Dictionary<Guid, ReciveMessage>();

            UserAvailable();
        }

        private void UserAvailable()
        {
            #region Send

            Receive<SendMessage>(message =>
            {
                _sentItems.Add(message.Message.Id, message);
                message.Destination.Tell(new ReciveMessage(Self, Phone, message.Message));
            });

            Receive<ReciveMessage>(message =>
            {
                message.SetAsDelivered();
                _rcvdItems.Add(message.Message.Id, message);
                Sender.Tell(new DeliverMessage(message.Message.Id));
            });

            Receive<DeliverMessage>(message =>
            {
                if (_sentItems.ContainsKey(message.MessageId))
                    _sentItems[message.MessageId].SetAsDelivered();
            });

            #endregion

            #region Edit

            Receive<EditMessage>(message =>
            {
                if (_sentItems.ContainsKey(message.MessageId))
                {
                    message.DestinationActor.Tell(new ReciveEditMessage(message.MessageId, message.Message));
                }
            });

            Receive<ReciveEditMessage>(message =>
            {
                if (_rcvdItems.ContainsKey(message.MessageId))
                    _rcvdItems[message.MessageId].SetMessage(message.Message);
            });

            Receive<AckReciveEditMessage>(message =>
            {
                if (_sentItems.ContainsKey(message.MessageId))
                    _sentItems[message.MessageId].SetMessage(message.Message);
            });

            #endregion

            #region Read

            Receive<ReadNewMessages>(message =>
            {
                var newMessages = _rcvdItems
                    .Select(a => a.Value)
                        .Where(a => a.Message.Status == Message.MessageStatus.Delivered)
                            .ToList();

                foreach(var msg in newMessages)
                {
                    msg.SetAsRead();
                    msg.SourceActor.Tell(new AckReadNewMessages(msg.Message.Id));
                }

                var result = newMessages.Select(a => new MessageResponse()
                {
                    CreatedDate = a.Message.CreatedDate,
                    ReciverPhone = Phone,
                    SenderPhone = a.SenderPhone,
                    Id = a.Message.Id,
                    ModifiedDate = a.Message.ModifiedDate,
                    Status = a.Message.Status,
                    Value = a.Message.Value,
                    DeliveredDate = a.Message.DeliveredDate,
                    ReadDate = a.Message.ReadDate
                });

                Sender.Tell(result);
            });

            Receive<ReadAllMessages>(message =>
            {
                var allMessages = _rcvdItems
                    .Select(a => a.Value).ToList();

                foreach (var msg in allMessages)
                {
                    if (msg.Message.Status != Message.MessageStatus.Read)
                    {
                        msg.SetAsRead();
                        msg.SourceActor.Tell(new AckReadNewMessages(msg.Message.Id));
                    }
                }

                var result = allMessages.Select(a => new MessageResponse()
                {
                    CreatedDate = a.Message.CreatedDate,
                    ReciverPhone = Phone,
                    SenderPhone = a.SenderPhone,
                    Id = a.Message.Id,
                    ModifiedDate = a.Message.ModifiedDate,
                    Status = a.Message.Status,
                    Value = a.Message.Value,
                    DeliveredDate = a.Message.DeliveredDate,
                    ReadDate = a.Message.ReadDate
                });

                Sender.Tell(result);
            });

            Receive<AckReadNewMessages>(message =>
            {
                if (_sentItems.ContainsKey(message.MessageId))
                    _sentItems[message.MessageId].SetAsRead();
            });

            #endregion
        }

        #region Messages

        public abstract class BaseMessage
        {
            public BaseMessage(Message message)
            {
                Message = message;
            }

            public Message Message { get; }

            public void SetAsDelivered()
            {
                Message.Status = Message.MessageStatus.Delivered;
                Message.DeliveredDate = DateTime.Now;
            }

            public void SetAsRead()
            {
                Message.Status = Message.MessageStatus.Read;
                Message.ReadDate = DateTime.Now;
            }

            public void SetMessage(string message)
            {
                Message.Value = message;
                Message.ModifiedDate = DateTime.Now;
            }
        }

        #region Send/Recive Classes

        public class SendMessage : BaseMessage
        {
            public SendMessage(IActorRef destination, Message message) : base(message)
            {
                Destination = destination;
            }

            public SendMessage(string destinationPhone, Message message) : base(message)
            {
                DestinationPhone = destinationPhone;
            }

            public IActorRef Destination { get; }
            public string DestinationPhone { get; }
        }

        public class DeliverMessage
        {
            public DeliverMessage(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; set; }
        }

        public class ReciveMessage : BaseMessage
        {
            public ReciveMessage(IActorRef sourceActor, string sourcePhone, Message message) : base(message)
            {
                SourceActor = sourceActor;
                SenderPhone = sourcePhone;
            }

            public IActorRef SourceActor { get; set; }
            public string SenderPhone { get; }
        }

        #endregion

        #region Edit Classes

        public class EditMessage
        {
            public EditMessage(Guid messageId, IActorRef destinationActor, string message)
            {
                MessageId = messageId;
                Message = message;
                DestinationActor = destinationActor;
            }

            public Guid MessageId { get; }
            public string Message { get; set; }
            public IActorRef DestinationActor { get; set; }
        }

        public class ReciveEditMessage
        {
            public ReciveEditMessage(Guid messageId, string message)
            {
                MessageId = messageId;
                Message = message;
            }

            public Guid MessageId { get; }
            public string Message { get; set; }
        }

        public class AckReciveEditMessage
        {
            public AckReciveEditMessage(Guid messageId, string message)
            {
                MessageId = messageId;
                Message = message;
            }

            public Guid MessageId { get; }
            public string Message { get; set; }
        }

        #endregion

        #region Read Classes

        public sealed class ReadAllMessages
        {
            public ReadAllMessages(string sourcePhone)
            {
                SourcePhone = sourcePhone;
            }

            public string SourcePhone { get; }
        }

        public sealed class ReadNewMessages
        {
            public ReadNewMessages(string sourcePhone)
            {
                SourcePhone = sourcePhone;
            }

            public string SourcePhone { get; }
        }

        public sealed class AckReadNewMessages
        {
            public AckReadNewMessages(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; }
        }

        #endregion

        #endregion
    }
}