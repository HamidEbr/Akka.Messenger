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
        private Dictionary<Guid, ReceiveSmsMessage> _rcvdSms;
        private readonly ActorRegistry _actorRegistry;
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private IActorRef _destinationActor; //only for testing
        
        public string PhoneNumber { get; }

        /// <summary>
        /// only used for test porposes
        /// </summary>
        /// <param name="userId">phone number of user</param>
        /// <param name="destinationActor">destination test probe</param>
        /// <returns></returns>
        public static Props Props(string userId, IActorRef destinationActor)
        {
            return Actor.Props.Create(() => new UserEntity(userId, destinationActor));
        }

        /// <summary>
        /// only used for test porposes
        /// </summary>
        /// <param name="userId">phone number of user</param>
        /// <param name="destinationActor">destination test probe</param>
        public UserEntity(string phoneNumber, IActorRef destinationActor)
        {
            _destinationActor = destinationActor;
            PhoneNumber = phoneNumber;
            Initializer();
        }
        
        public static Props Props(string userId)
        {
            return Actor.Props.Create(() => new UserEntity(userId));
        }

        public UserEntity(string phoneNumber)
        {
            _actorRegistry = ActorRegistry.For(Context.System);
            PhoneNumber = phoneNumber;
            Initializer();
        }

        private void Initializer()
        {
            _sentSms = new Dictionary<Guid, SendSmsMessage>();
            _rcvdSms = new Dictionary<Guid, ReceiveSmsMessage>();

            #region Send

            Receive<SendSmsMessage>(message =>
            {
                _sentSms.Add(message.Sms.Id, message);
                DestinationActor()
                    .Tell(new ShardEnvelope(message.DestinationPhone, ReceiveSmsMessage.Create(PhoneNumber, message.Sms)));
                Sender.Tell(message.Sms.Id);
            });

            Receive<ReceiveSmsMessage>(message =>
            {
                message.SetAsDelivered();
                _rcvdSms.Add(message.Sms.Id, message);
                DestinationActor()
                    .Tell(new ShardEnvelope(message.SenderPhone, DeliverMessage.Create(message.Sms.Id)));
                Sender.Tell(ReceiveSmsMessageSuccess.Create(message.Sms.Id));
            });

            Receive<DeliverMessage>(message =>
            {
                if (_sentSms.ContainsKey(message.MessageId))
                {
                    _sentSms[message.MessageId].SetAsDelivered();
                    Sender.Tell(DeliverMessageSuccess.Instance());
                }
                else
                {
                    Sender.Tell(SmsNotFound.Create(message.MessageId));
                }
            });

            #endregion

            #region Edit

            ReceiveAsync<EditSmsMessage>(async message =>
            {
                if (_sentSms.ContainsKey(message.SmsId))
                {
                    var response = await DestinationActor().Ask<SmsResponse>(
                        new ShardEnvelope(message.DestinationPhone, ReciveEditSmsMessage.Create(message.SmsId, message.Message)));

                    _sentSms[message.SmsId].SetMessage(message.Message);
                    Sender.Tell(response);
                }
                else
                {
                    Sender.Tell(SmsNotFound.Create(message.SmsId));
                }
            });

            Receive<ReciveEditSmsMessage>(message =>
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
                        ReceiverPhone = PhoneNumber,
                        SenderPhone = rcvdSms.SenderPhone,
                        Status = rcvdSms.Sms.Status,
                        Text = rcvdSms.Sms.Text,
                    });
                }
                else
                {
                    Sender.Tell(SmsNotFound.Create(message.SmsId));
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
                    DestinationActor()
                        .Tell(new ShardEnvelope(msg.SenderPhone, AckReadNewSmsMessage.Create(msg.Sms.Id)));
                }

                var result = newMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReceiverPhone = PhoneNumber,
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
                        DestinationActor()
                            .Tell(new ShardEnvelope(msg.SenderPhone, AckReadNewSmsMessage.Create(msg.Sms.Id)));
                    }
                }

                var result = allMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReceiverPhone = PhoneNumber,
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
                {
                    _sentSms[message.MessageId].SetAsRead();
                    Sender.Tell(AckReadNewSmsMessageSuccess.Create(message.MessageId));
                }
                else
                {
                    Sender.Tell(SmsNotFound.Create(message.MessageId));
                }
            });

            #endregion

            Receive<object>(o =>
            {
            });
        }

        private IActorRef DestinationActor()
        {
            if (_destinationActor != null)
                return _destinationActor;
            return _actorRegistry.Get<UserEntity>();
        }

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

        public sealed class ReceiveSmsMessage : BaseMessage
        {
            public static ReceiveSmsMessage Create(string senderPhone, Sms message)
            {
                return new ReceiveSmsMessage(senderPhone, message);
            }

            private ReceiveSmsMessage(string senderPhone, Sms message) : base(message)
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

        #region Success Responses
        
        public abstract class BaseSuccessResponse
        {
        }

        public sealed class AckReadNewSmsMessageSuccess : BaseSuccessResponse
        {
            public Guid Id { get; }

            public static AckReadNewSmsMessageSuccess Create(Guid id) => new(id);

            private AckReadNewSmsMessageSuccess(Guid id)
            {
                Id = id;
            }
        }

        public sealed class ReceiveSmsMessageSuccess : BaseSuccessResponse
        {
            public Guid Id { get; }
            
            public static ReceiveSmsMessageSuccess Create(Guid id) => new(id);

            private ReceiveSmsMessageSuccess(Guid id)
            {
                Id = id;
            }
        }        

        public sealed class DeliverMessageSuccess : BaseSuccessResponse
        {
            public static DeliverMessageSuccess Instance() => new();

            private DeliverMessageSuccess()
            {
            }
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
            public Guid Id { get; }

            public static SmsNotFound Create(Guid id) => new(id);
            
            private SmsNotFound(Guid id) : base($"Sms {id} Not found!") {
                Id = id;
            }
        }

        #endregion

        #endregion
    }
}