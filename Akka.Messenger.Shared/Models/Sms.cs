namespace Akka.Messenger.Shared.Models
{
    public class Sms
    {
        public enum SmsStatus
        {
            Sent,
            Delivered,
            Read
        }

        public Sms(string value)
        {
            Id = Guid.NewGuid();
            Text = value;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Status = SmsStatus.Sent;
        }

        public Guid Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public SmsStatus Status { get; set; }

        public override string ToString()
        {
            return $"{Id}\t{Text}\t{Status}";
        }
    }
}
