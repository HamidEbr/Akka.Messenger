using Akka.Actor;
using Service.Api.Helper;
using Service.Api.Models;
using Service.Api.Services;

namespace Service.Api.Actors
{
    public class User : ReceiveActor
    {
        private Dictionary<Guid, SendSmsMessage> _sentSmse;
        private Dictionary<Guid, ReciveSmsMessage> _rcvdSms;
        private readonly IMessageSessionHandler _messageSessionHandler;
        
        public string Phone { get; }


        public User(IMessageSessionHandler messageSessionHandler, string userPhone)
        {
            Phone = userPhone;
            _messageSessionHandler = messageSessionHandler;
            _sentSmse = new Dictionary<Guid, SendSmsMessage>();
            _rcvdSms = new Dictionary<Guid, ReciveSmsMessage>();
            UserAvailable();
        }

        private void UserAvailable()
        {
            #region Send

            Receive<SendSmsMessage>(message =>
            {
                _sentSmse.Add(message.Sms.Id, message);
                _messageSessionHandler.ShardRegion.Tell(new ShardEnvelope(message.DestinationPhone, new ReciveSmsMessage(Phone, message.Sms)));
                Sender.Tell(message.Sms.Id);
            });

            Receive<ReciveSmsMessage>(message =>
            {
                message.SetAsDelivered();
                _rcvdSms.Add(message.Sms.Id, message);
                _messageSessionHandler.ShardRegion.Tell(
                    new ShardEnvelope(message.SenderPhone, new DeliverMessage(message.Sms.Id)));
            });

            Receive<DeliverMessage>(message =>
            {
                if (_sentSmse.ContainsKey(message.MessageId))
                    _sentSmse[message.MessageId].SetAsDelivered();
            });

            #endregion

            #region Edit

            Receive<EditSmsMessage>(message =>
            {
                if (_sentSmse.ContainsKey(message.SmsId))
                {
                    var response = _messageSessionHandler.ShardRegion.Ask<SmsResponse>(
                        new ShardEnvelope(message.DestinationPhone, new ReciveEditSmsMessage(message.SmsId, message.Message)));
                    Sender.Tell(response);
                }
            });

            Receive<ReciveEditSmsMessage>(message =>
            {
                if (_rcvdSms.TryGetValue(message.SmsId, out var rcvdSms)) 
                {
                    _rcvdSms[message.SmsId].SetMessage(message.Text);
                    
                    _messageSessionHandler.ShardRegion.Tell(
                        new ShardEnvelope(rcvdSms.SenderPhone, new AckReciveEditSmsMessage(message.SmsId, message.Text)));

                    Sender.Tell(new SmsResponse()
                    {
                        Id = rcvdSms.Sms.Id,
                        CreatedDate = rcvdSms.Sms.CreatedDate,
                        DeliveredDate = rcvdSms.Sms.DeliveredDate,
                        ModifiedDate = rcvdSms.Sms.ModifiedDate,
                        ReadDate = rcvdSms.Sms.ReadDate,
                        ReciverPhone = Phone,
                        SenderPhone = rcvdSms.SenderPhone,
                        Status = rcvdSms.Sms.Status,
                        Text = rcvdSms.Sms.Text,
                    });
                }
            });

            Receive<AckReciveEditSmsMessage>(message =>
            {
                if (_sentSmse.ContainsKey(message.MessageId))
                    _sentSmse[message.MessageId].SetMessage(message.Message);
            });

            #endregion

            #region Read

            Receive<ReadNewSmsesMessage>(message =>
            {
                var newMessages = _rcvdSms
                    .Select(a => a.Value)
                        .Where(a => a.Sms.Status == Sms.SmsStatus.Delivered)
                            .ToList();

                foreach(var msg in newMessages)
                {
                    msg.SetAsRead();
                    _messageSessionHandler.ShardRegion.Tell(new ShardEnvelope(msg.SenderPhone, new AckReadNewSmsMessage(msg.Sms.Id)));
                }

                var result = newMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReciverPhone = Phone,
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
                        _messageSessionHandler.ShardRegion.Tell(new ShardEnvelope(msg.SenderPhone, new AckReadNewSmsMessage(msg.Sms.Id)));
                    }
                }

                var result = allMessages.Select(a => new SmsResponse()
                {
                    CreatedDate = a.Sms.CreatedDate,
                    ReciverPhone = Phone,
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
                if (_sentSmse.ContainsKey(message.MessageId))
                    _sentSmse[message.MessageId].SetAsRead();
            });

            #endregion
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
            public SendSmsMessage(string destinationPhone, Sms message) : base(message)
            {
                DestinationPhone = destinationPhone;
            }

            public string DestinationPhone { get; }
        }

        public sealed class DeliverMessage
        {
            public DeliverMessage(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; }
        }

        public sealed class ReciveSmsMessage : BaseMessage
        {
            public ReciveSmsMessage(string senderPhone, Sms message) : base(message)
            {
                SenderPhone = senderPhone;
            }

            public string SenderPhone { get; }
        }

        #endregion

        #region Edit Classes

        public sealed class EditSmsMessage
        {
            public EditSmsMessage(Guid messageId, string destinationPhone, string message)
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
            public ReciveEditSmsMessage(Guid messageId, string message)
            {
                SmsId = messageId;
                Text = message;
            }

            public Guid SmsId { get; }
            public string Text { get; }
        }

        public sealed class AckReciveEditSmsMessage
        {
            public AckReciveEditSmsMessage(Guid messageId, string message)
            {
                MessageId = messageId;
                Message = message;
            }

            public Guid MessageId { get; }
            public string Message { get; }
        }

        #endregion

        #region Read Classes

        public sealed class ReadAllSmsesMessage
        {
        }

        public sealed class ReadNewSmsesMessage
        {
        }

        public sealed class AckReadNewSmsMessage
        {
            public AckReadNewSmsMessage(Guid messageId)
            {
                MessageId = messageId;
            }

            public Guid MessageId { get; }
        }

        #endregion

        #endregion
    }
}