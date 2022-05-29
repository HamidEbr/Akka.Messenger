using Akka.Actor;
using Akka.Event;
using Akka.Hosting;
using Akka.Messenger.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akka.Messenger.Shared.Sharding
{
    public class UserEntity : ReceiveActor
    {
        private Dictionary<Guid, SendSmsMessage> _sentSms;
        private Dictionary<Guid, ReciveSmsMessage> _rcvdSms;

        public static Props Props(string userId)
        {
            return Actor.Props.Create(() => new UserEntity(userId));
        }

        private UserEntity(string phoneNumber)
        {
            _sentSms = new Dictionary<Guid, SendSmsMessage>();
            _rcvdSms = new Dictionary<Guid, ReciveSmsMessage>();
            PhoneNumber = phoneNumber;
            _actorRegistry = ActorRegistry.For(Context.System);

            #region Send

            Receive<SendSmsMessage>(message =>
            {
                _sentSms.Add(message.Sms.Id, message);
                _actorRegistry.Get<UserEntity>()
                    .Tell(new ShardEnvelope(message.DestinationPhone, ReciveSmsMessage.Create(PhoneNumber, message.Sms)));
                Sender.Tell(message.Sms.Id);
            });

            Receive<ReciveSmsMessage>(message =>
            {
                message.SetAsDelivered();
                _rcvdSms.Add(message.Sms.Id, message);
                _actorRegistry.Get<UserEntity>()
                    .Tell(new ShardEnvelope(message.SenderPhone, DeliverMessage.Create(message.Sms.Id)));
            });

            Receive<DeliverMessage>(message =>
            {
                if (_sentSms.ContainsKey(message.MessageId))
                    _sentSms[message.MessageId].SetAsDelivered();
            });

            #endregion

            #region Edit

            ReceiveAsync<EditSmsMessage>(async message =>
            {
                if (_sentSms.ContainsKey(message.SmsId))
                {
                    var response = await _actorRegistry.Get<UserEntity>().Ask<SmsResponse>(
                        new ShardEnvelope(message.DestinationPhone, ReciveEditSmsMessage.Create(message.SmsId, message.Message)));

                    if (_sentSms.ContainsKey(response.Id))
                        _sentSms[message.SmsId].SetMessage(message.Message);

                    Sender.Tell(response);
                }
                else
                {
                    Sender.Tell(SmsNotFound.Instance());
                }
            });

            ReceiveAsync<ReciveEditSmsMessage>(async message =>
            {
                if (_rcvdSms.TryGetValue(message.SmsId, out var rcvdSms))
                {
                    _rcvdSms[message.SmsId].SetMessage(message.Text);

                    Sender.Tell(new SmsResponse()
                    {
                        Id = rcvdSms.Sms.Id,
                        CreatedDate = rcvdSms.Sms.CreatedDate,
                        DeliveredDate = rcvdSms.Sms.DeliveredDate,
                        ModifiedDate = rcvdSms.Sms.ModifiedDate,
                        ReadDate = rcvdSms.Sms.ReadDate,
                        ReciverPhone = PhoneNumber,
                        SenderPhone = rcvdSms.SenderPhone,
                        Status = rcvdSms.Sms.Status,
                        Text = rcvdSms.Sms.Text,
                    });
                }
            });

            #endregion

            #region Read

            Receive<ReadNewSmsesMessage>(message =>
            {
                var newMessages = _rcvdSms
                    .Select(a => a.Value)
                        .Where(a => a.Sms.Status == Sms.SmsStatus.Delivered)
                            .ToList();

                foreach (var msg in newMessages)
                {
                    msg.SetAsRead();
                    _actorRegistry.Get<UserEntity>()
                        .Tell(new ShardEnvelope(msg.SenderPhone, AckReadNewSmsMessage.Create(msg.Sms.Id)));
                }

                var result = newMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReciverPhone = PhoneNumber,
                    SenderPhone = a.SenderPhone,
                    Id = a.Sms.Id,
                    ModifiedDate = a.Sms.ModifiedDate,
                    Status = a.Sms.Status,
                    Text = a.Sms.Text,
                    DeliveredDate = a.Sms.DeliveredDate,
                    ReadDate = a.Sms.ReadDate
                });

                Sender.Tell(result);
            });

            Receive<ReadAllSmsesMessage>(message =>
            {
                var allMessages = _rcvdSms
                    .Select(a => a.Value).ToList();

                foreach (var msg in allMessages)
                {
                    if (msg.Sms.Status != Sms.SmsStatus.Read)
                    {
                        msg.SetAsRead();
                        _actorRegistry.Get<UserEntity>()
                            .Tell(new ShardEnvelope(msg.SenderPhone, AckReadNewSmsMessage.Create(msg.Sms.Id)));
                    }
                }

                var result = allMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReciverPhone = PhoneNumber,
                    SenderPhone = a.SenderPhone,
                    Id = a.Sms.Id,
                    ModifiedDate = a.Sms.ModifiedDate,
                    Status = a.Sms.Status,
                    Text = a.Sms.Text,
                    DeliveredDate = a.Sms.DeliveredDate,
                    ReadDate = a.Sms.ReadDate
                });

                Sender.Tell(result);
            });

            Receive<AckReadNewSmsMessage>(message =>
            {
                if (_sentSms.ContainsKey(message.MessageId))
                    _sentSms[message.MessageId].SetAsRead();
            });

            #endregion

            Receive<object>(o =>
            {
            });
        }

        private readonly ILoggingAdapter _log = Context.GetLogger();

        public string PhoneNumber { get; }

        private readonly ActorRegistry _actorRegistry;

        #region Messages

        public abstract class BaseMessage
        {
            public BaseMessage(Sms message)
            {
                Sms = message;
            }

            public Sms Sms { get; }

            public void SetAsDelivered()
            {
                Sms.Status = Sms.SmsStatus.Delivered;
                Sms.DeliveredDate = DateTime.Now;
            }

            public void SetAsRead()
            {
                Sms.Status = Sms.SmsStatus.Read;
                Sms.ReadDate = DateTime.Now;
            }

            public void SetMessage(string message)
            {
                Sms.Text = message;
                Sms.ModifiedDate = DateTime.Now;
            }
        }

        #region Send/Recive Classes

        public sealed class SendSmsMessage : BaseMessage
        {
            public static SendSmsMessage Create(string destinationPhone, Sms message)
            {
                return new SendSmsMessage(destinationPhone, message);
            }
                
            private SendSmsMessage(string destinationPhone, Sms message) : base(message)
            {
                DestinationPhone = destinationPhone;
            }

            public string DestinationPhone { get; }
        }

        public sealed class DeliverMessage
        {
            public static DeliverMessage Create(Guid messageId)
            {
                return new DeliverMessage(messageId);
            }

            private DeliverMessage(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; }
        }

        public sealed class ReciveSmsMessage : BaseMessage
        {
            public static ReciveSmsMessage Create(string senderPhone, Sms message)
            {
                return new ReciveSmsMessage(senderPhone, message);
            }

            private ReciveSmsMessage(string senderPhone, Sms message) : base(message)
            {
                SenderPhone = senderPhone;
            }

            public string SenderPhone { get; }
        }

        #endregion

        #region Edit Classes

        public sealed class EditSmsMessage
        {
            public static EditSmsMessage Create(Guid messageId, string destinationPhone, string message)
            {
                return new EditSmsMessage(messageId, destinationPhone, message);
            }

            private EditSmsMessage(Guid messageId, string destinationPhone, string message)
            {
                SmsId = messageId;
                Message = message;
                DestinationPhone = destinationPhone;
            }

            public Guid SmsId { get; }
            public string Message { get; }
            public string DestinationPhone { get; }
        }

        public sealed class ReciveEditSmsMessage
        {
            public static ReciveEditSmsMessage Create(Guid messageId, string message)
            {
                return new ReciveEditSmsMessage(messageId, message);
            }

            private ReciveEditSmsMessage(Guid messageId, string message)
            {
                SmsId = messageId;
                Text = message;
            }

            public Guid SmsId { get; }
            public string Text { get; }
        }

        #endregion

        #region Read Classes

        public sealed class ReadAllSmsesMessage
        {
            public static ReadAllSmsesMessage Instance() => new ReadAllSmsesMessage();
            
            private ReadAllSmsesMessage()
            {
            }
        }

        public sealed class ReadNewSmsesMessage
        {
            public static ReadNewSmsesMessage Instance() => new ReadNewSmsesMessage();

            private ReadNewSmsesMessage()
            {
            }
        }

        public sealed class AckReadNewSmsMessage
        {
            public static AckReadNewSmsMessage Create(Guid messageId) => new AckReadNewSmsMessage(messageId);

            private AckReadNewSmsMessage(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; }
        }

        #endregion

        #region Error Responses

        public abstract class BaseErrorResponse
        {
            public BaseErrorResponse(string message)
            {
                Message = message;
            }

            public string Message { get; }
        }

        public sealed class SmsNotFound : BaseErrorResponse
        {
            public static SmsNotFound Instance() => new();
            
            private SmsNotFound() : base("Sms Not found!") { }
        }

        #endregion

        #endregion
    }
}