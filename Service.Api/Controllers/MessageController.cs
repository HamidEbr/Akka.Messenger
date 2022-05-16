//#define Sharding
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
#if Sharding
            Guid result = await _messageSessionHandler.Ask<Guid>(new ShardEnvelope(sourcePhone,
                //new User.SendMessage(destinationPhone, new Message(message.Message))));
                new ProcessesCoordinator.RawSendMessage(sourcePhone, new Message(message.Message), destinationPhone)));
#else
            Guid result = await _messageSessionHandler.Ask<Guid>(new ProcessesCoordinator.RawSendMessage(sourcePhone, 
                new Message(message.Message), destinationPhone));
#endif
            return new OkObjectResult(result);
        }

        [HttpPost("{id}/edit/source/{sourcePhone}/destination/{destinationPhone}")]
        public IActionResult EditMessage(Guid id, string sourcePhone, string destinationPhone, [FromBody] MessageDto message)
        {
#if Sharding
            _messageSessionHandler.Handle(new ShardEnvelope(sourcePhone,
                new ProcessesCoordinator.RawEditMessage(id, sourcePhone, message.Message, destinationPhone)));
#else
            _messageSessionHandler.Handle(
                new ProcessesCoordinator.RawEditMessage(id, sourcePhone, message.Message, destinationPhone));
#endif
            return new OkResult();
        }

        [HttpGet("read_new_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<MessageResponse>> ReadNewMessages(string sourcePhone)
        {
#if Sharding
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(new ShardEnvelope(sourcePhone,
                new ProcessesCoordinator.RawReadNewMessages(sourcePhone)));
#else
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(
                new ProcessesCoordinator.RawReadNewMessages(sourcePhone));
#endif
            return result;
        }

        [HttpGet("read_all_messages/source/{sourcePhone}")]
        public async Task<IEnumerable<MessageResponse>> ReadAllMessages(string sourcePhone)
        {
#if Sharding
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(
                new ShardEnvelope(sourcePhone, new ProcessesCoordinator.RawReadAllMessages(sourcePhone)));
#else
            var result = await _messageSessionHandler.Ask<IEnumerable<MessageResponse>>(
                new ProcessesCoordinator.RawReadAllMessages(sourcePhone));
#endif
            return result;
        }
    }
}
