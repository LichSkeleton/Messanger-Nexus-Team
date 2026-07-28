namespace NexusTeam.Client.Services
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for managing voice calls via WebRTC.
    /// </summary>
    public interface ICallService
    {
        /// <summary>
        /// Gets a value indicating whether there is an active call.
        /// </summary>
        bool IsInCall { get; }

        /// <summary>
        /// Gets the current call identifier, or null if no active call.
        /// </summary>
        string? CurrentCallId { get; }

        /// <summary>
        /// Gets the current call user identifier, or null if no active call.
        /// </summary>
        string? CurrentCallUserId { get; }

        /// <summary>
        /// Gets the current call state.
        /// </summary>
        CallState CurrentCallState { get; }

        /// <summary>
        /// Occurs when an incoming call is received.
        /// </summary>
        event EventHandler<IncomingCallEventArgs>? IncomingCall;

        /// <summary>
        /// Occurs when the call state changes.
        /// </summary>
        event EventHandler<CallStateChangedEventArgs>? CallStateChanged;

        /// <summary>
        /// Occurs when a call ends.
        /// </summary>
        event EventHandler<string>? CallEnded;

        /// <summary>
        /// Starts a voice call to the specified user.
        /// </summary>
        /// <param name="userId">The user identifier to call.</param>
        /// <param name="chatId">Optional chat identifier if call is from a chat.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task StartCallAsync(string userId, string? chatId = null);

        /// <summary>
        /// Answers an incoming call.
        /// </summary>
        /// <param name="callId">The call identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AnswerCallAsync(string callId);

        /// <summary>
        /// Rejects an incoming call.
        /// </summary>
        /// <param name="callId">The call identifier.</param>
        /// <param name="reason">Optional reason for rejection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RejectCallAsync(string callId, string? reason = null);

        /// <summary>
        /// Ends the current call.
        /// </summary>
        /// <param name="callId">The call identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EndCallAsync(string callId);
    }
}
