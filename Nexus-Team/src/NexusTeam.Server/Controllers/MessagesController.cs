namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// Controller for message-related endpoints (search, edit, delete).
    /// </summary>
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService messageService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagesController"/> class.
        /// </summary>
        /// <param name="messageService">The message service.</param>
        public MessagesController(IMessageService messageService)
        {
            this.messageService = messageService;
        }

        /// <summary>
        /// Search messages within a specific chat.
        /// </summary>
        /// <param name="chatId">The chat ID to search in.</param>
        /// <param name="query">The text to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of matching messages.</returns>
        [HttpGet("{chatId}/search")]
        [ProducesResponseType(typeof(IEnumerable<MessageDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<MessageDto>>> SearchMessages(
            string chatId,
            [FromQuery] string query,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return this.BadRequest("Search query cannot be empty.");
            }

            var messages = await this.messageService.SearchMessagesAsync(chatId, query, cancellationToken);
            return this.Ok(messages);
        }
    }
}
