namespace Service.Api.Models
{
    public class SmsDto
    {
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Message { get; set; }
    }
}
