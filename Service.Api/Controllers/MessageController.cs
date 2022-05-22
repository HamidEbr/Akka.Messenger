using Microsoft.AspNetCore.Mvc;
using Service.Api.Actors;
using Service.Api.Helper;
using Service.Api.Models;
using Service.Api.Services;

namespace Service.Api.Controllers
{
    [ApiController]
    [Route("message")]
    public class MessageController
    {
        private readonly IMessageSessionHandler _messageSessionHandler;

        public MessageController(IMessageSessionHandler messageSessionHandler)
        {
            _messageSessionHandler = messageSessionHandler;
        }

        [HttpPost("send/source/{sourcePhone}/destination/{destinationPhone}")]
        public async Task<IActionResult> SendMessage(string sourcePhone, string destinationPhone, [FromBody] SmsDto message)
        {
            Guid result = await _messageSessionHandler.Ask<Guid>(new ShardEnvelope(sourcePhone,
                //new User.SendMessage(destinationPhone, new Message(message.Message))));
                new User.SendSmsMessage(destinationPhone, new Sms(message.Message))));
            return new OkObjectResult(result);
        }

        [HttpPost("{id}/edit/source/{sourcePhone}/destination/{destinationPhone}")]
        public IActionResult EditMessage(Guid id, string sourcePhone, string destinationPhone, [FromBody] SmsDto message)
        {
            _messageSessionHandler.Tell(new ShardEnvelope(sourcePhone,
                new User.EditSmsMessage(id, destinationPhone, message.Message)));
            return new OkResult();
        }

        [HttpGet("read_new_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<SmsResponse>> ReadNewMessages(string sourcePhone)
        {
            var result = await _messageSessionHandler.Ask<IEnumerable<SmsResponse>>(new ShardEnvelope(sourcePhone,
                new User.ReadNewSmsesMessage()));
            return result;
        }

        [HttpGet("read_all_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<SmsResponse>> ReadAllMessages(string sourcePhone)
        {
            var result = await _messageSessionHandler.Ask<IEnumerable<SmsResponse>>(
                new ShardEnvelope(sourcePhone, new User.ReadAllSmsesMessage()));
            return result;
        }
    }
}
