namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Represents the state of a call.
    /// </summary>
    public enum CallState
    {
        /// <summary>
        /// No active call.
        /// </summary>
        Idle,

        /// <summary>
        /// Call is being initiated.
        /// </summary>
        Initiating,

        /// <summary>
        /// Call is ringing (waiting for answer).
        /// </summary>
        Ringing,

        /// <summary>
        /// Call is connecting (WebRTC negotiation in progress).
        /// </summary>
        Connecting,

        /// <summary>
        /// Call is connected and active.
        /// </summary>
        Connected,

        /// <summary>
        /// Call is ending.
        /// </summary>
        Ending,
    }

    /// <summary>
    /// Event arguments for call state change events.
    /// </summary>
    public class CallStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the unique call identifier.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current call state.
        /// </summary>
        public CallState State { get; set; }
    }
}
