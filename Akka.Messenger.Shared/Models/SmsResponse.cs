namespace Akka.Messenger.Shared.Models
{
    public class SmsResponse
    {
        public SmsResponse()
        {
        }

        public Guid Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Sms.SmsStatus Status { get; set; }
        public string SenderPhone { get; set; }
        public string ReceiverPhone { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }

        public override string ToString()
        {
            return $"{Id}\t{Text}\t{Status}";
        }
    }
}
