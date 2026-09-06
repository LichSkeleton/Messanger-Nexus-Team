namespace NexusTeam.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// Controller for call-related helper endpoints.
    /// </summary>
    [ApiController]
    [Route("api/calls")]
    public class CallsController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private readonly ICallHistoryService callHistoryService;
        private readonly IChatService chatService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallsController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="callHistoryService">Call history service.</param>
        /// <param name="chatService">Chat service, used to verify chat membership before exposing history.</param>
        public CallsController(IConfiguration configuration, ILogger logger, ICallHistoryService callHistoryService, IChatService chatService)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.callHistoryService = callHistoryService;
            this.chatService = chatService;
        }

        /// <summary>
        /// Records the outcome of a call (completed/rejected/missed/failed) reported by a client.
        /// </summary>
        /// <param name="request">The call outcome details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The recorded call history entry, or 401/403 if the caller is not a participant.</returns>
        [HttpPost("history")]
        [ProducesResponseType(typeof(CallHistoryDto), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> RecordHistory([FromBody] RecordCallHistoryRequest request, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var dto = await this.callHistoryService.RecordAsync(userId, request, cancellationToken);
            if (dto == null)
            {
                return this.Forbid();
            }

            return this.Ok(dto);
        }

        /// <summary>
        /// Gets recent call history for a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <param name="limit">The maximum number of entries to return.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat's recent call history, or 401/403 if the caller is not a participant.</returns>
        [HttpGet("history/{chatId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetHistory(string chatId, [FromQuery] int limit = 30, CancellationToken cancellationToken = default)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var chat = await this.chatService.GetChatByIdAsync(chatId, userId, cancellationToken);
            if (chat == null)
            {
                return this.Forbid();
            }

            var history = await this.callHistoryService.GetByChatIdAsync(chatId, limit, cancellationToken);
            return this.Ok(history);
        }

        /// <summary>
        /// Gets missed calls the current user hasn't seen yet (shown as a toast on reconnect).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The caller's unseen missed calls, or 401 if unauthenticated.</returns>
        [HttpGet("history/missed/unseen")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetUnseenMissed(CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var missed = await this.callHistoryService.GetUnseenMissedAsync(userId, cancellationToken);
            return this.Ok(missed);
        }

        /// <summary>
        /// Marks a missed-call entry as seen.
        /// </summary>
        /// <param name="id">The history entry identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>204 on success, or 401/403 if the caller is not the callee on that entry.</returns>
        [HttpPost("history/{id}/seen")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> MarkSeen(string id, CancellationToken cancellationToken)
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var marked = await this.callHistoryService.MarkSeenAsync(id, userId, cancellationToken);
            if (!marked)
            {
                return this.Forbid();
            }

            return this.NoContent();
        }

        /// <summary>
        /// Gets ICE server configuration for WebRTC calls.
        /// </summary>
        /// <returns>ICE server list with optional TURN credentials.</returns>
        [HttpGet("ice-servers")]
        [ProducesResponseType(typeof(IceServersResponse), 200)]
        [ProducesResponseType(401)]
        public ActionResult<IceServersResponse> GetIceServers()
        {
            var userId = this.HttpContext.Items["UserId"] as string;
            if (string.IsNullOrEmpty(userId))
            {
                return this.Unauthorized();
            }

            var requestHost = this.HttpContext.Request.Host.Host;
            if (string.IsNullOrWhiteSpace(requestHost))
            {
                requestHost = "localhost";
            }

            var turnSecret = this.configuration["TurnSecret"];
            var expires = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
            var iceServers = new List<IceServerDto>
            {
                new IceServerDto
                {
                    Urls = new[] { "stun:stun.l.google.com:19302" },
                },
            };

            if (!string.IsNullOrWhiteSpace(turnSecret))
            {
                var username = $"{userId}:{expires}";
                var credential = ComputeHmacSha1Base64(turnSecret, username);

                iceServers.Add(new IceServerDto
                {
                    Urls = new[]
                    {
                        $"turn:{requestHost}:3478?transport=udp",
                        $"turn:{requestHost}:5349?transport=tcp",
                    },
                    Username = username,
                    Credential = credential,
                });
            }
            else
            {
                this.logger.Warning("TurnSecret is not configured; ICE server response will omit TURN credentials.");
            }

            return this.Ok(new IceServersResponse { IceServers = iceServers });
        }

        private static string ComputeHmacSha1Base64(string key, string text)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var textBytes = Encoding.UTF8.GetBytes(text);
            using var hmac = new HMACSHA1(keyBytes);
            var hash = hmac.ComputeHash(textBytes);
            return Convert.ToBase64String(hash);
        }

        public sealed class IceServerDto
        {
            public IEnumerable<string> Urls { get; set; } = Array.Empty<string>();

            public string? Username { get; set; }

            public string? Credential { get; set; }
        }

        public sealed class IceServersResponse
        {
            public IEnumerable<IceServerDto> IceServers { get; set; } = Array.Empty<IceServerDto>();
        }
    }
}
