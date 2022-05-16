namespace Service.Api.Models
{
    public class Message
    {
        public enum MessageStatus
        {
            Sent,
            Delivered,
            Read
        }

        public Message(string value)
        {
            Id = Guid.NewGuid();
            Value = value;
            CreatedDate = DateTime.Now;
            ModifiedDate = DateTime.Now;
            Status = MessageStatus.Sent;
        }

        public Guid Id { get; set; }
        public string Value { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }
        public MessageStatus Status { get; set; }

        public override string ToString()
        {
            return $"{Id}\t{Value}\t{Status}";
        }
    }
}
