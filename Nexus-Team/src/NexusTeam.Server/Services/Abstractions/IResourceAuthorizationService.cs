namespace NexusTeam.Server.Services.Abstractions
{
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Models;
    using SharedChat = NexusTeam.Shared.Models.Chat;
    using SharedMessage = NexusTeam.Shared.Models.Message;

    /// <summary>
    /// Reusable authorization checks for chat membership and resource ownership.
    /// </summary>
    public interface IResourceAuthorizationService
    {
        /// <summary>
        /// Ensures the user is a participant of the chat.
        /// </summary>
        /// <param name="chatId">Chat ID.</param>
        /// <param name="userId">Caller user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat when authorized.</returns>
        Task<SharedChat> EnsureChatParticipantAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the user may delete the chat. Direct messages allow any participant;
        /// groups and channels require ownership (CreatedBy).
        /// </summary>
        /// <param name="chatId">Chat ID.</param>
        /// <param name="userId">Caller user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The chat when authorized.</returns>
        Task<SharedChat> EnsureChatOwnerForDeleteAsync(string chatId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the user may access a message via its owning chat membership.
        /// </summary>
        /// <param name="messageId">Message ID.</param>
        /// <param name="userId">Caller user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The message when authorized.</returns>
        Task<SharedMessage> EnsureMessageChatParticipantAsync(string messageId, string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ensures the user owns a generated image.
        /// </summary>
        /// <param name="imageId">Generated image ID.</param>
        /// <param name="userId">Caller user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The image when authorized.</returns>
        Task<GeneratedImage> EnsureGeneratedImageOwnerAsync(string imageId, string userId, CancellationToken cancellationToken = default);
    }
}
