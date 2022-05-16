namespace Service.Api.Models
{
    public class MessageResponse
    {
        public MessageResponse()
        {
        }

        public Guid Id { get; set; }
        public string Value { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public Message.MessageStatus Status { get; set; }
        public string SenderPhone { get; set; }
        public string ReciverPhone { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? ReadDate { get; set; }

        public override string ToString()
        {
            return $"{Id}\t{Value}\t{Status}";
        }
    }
}
