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
        public async Task<IActionResult> SendMessage(string sourcePhone, string destinationPhone, [FromBody] MessageDto message)
        {
            Guid result = await _messageSessionHandler.Ask<Guid>(new ShardEnvelope(sourcePhone,
                //new User.SendMessage(destinationPhone, new Message(message.Message))));
                new User.SendMessage(destinationPhone, new Message(message.Message))));
            return new OkObjectResult(result);
        }

        [HttpPost("{id}/edit/source/{sourcePhone}/destination/{destinationPhone}")]
        public IActionResult EditMessage(Guid id, string sourcePhone, string destinationPhone, [FromBody] MessageDto message)
        {
            _messageSessionHandler.Handle(new ShardEnvelope(sourcePhone,
                new User.EditMessage(id, destinationPhone, message.Message)));
            return new OkResult();
        }

        [HttpGet("read_new_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<MessageResponse>> ReadNewMessages(string sourcePhone)
        {
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(new ShardEnvelope(sourcePhone,
                new User.ReadNewMessages()));
            return result;
        }

        [HttpGet("read_all_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<MessageResponse>> ReadAllMessages(string sourcePhone)
        {
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(
                new ShardEnvelope(sourcePhone, new User.ReadAllMessages()));
            return result;
        }
    }
}
