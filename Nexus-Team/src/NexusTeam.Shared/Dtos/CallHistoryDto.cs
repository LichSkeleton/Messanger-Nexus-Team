namespace NexusTeam.Shared.Dtos
{
    using System;

    /// <summary>
    /// DTO representing a call history entry returned to clients.
    /// </summary>
    public class CallHistoryDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the history entry.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call correlation ID shared by both peers during signaling.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat this call belongs to.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who initiated the call.
        /// </summary>
        public string CallerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who was called.
        /// </summary>
        public string CalleeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call type ("Audio" or "Video").
        /// </summary>
        public string CallType { get; set; } = "Audio";

        /// <summary>
        /// Gets or sets the call outcome status: Completed, Rejected, Missed, NoAnswer, or Failed.
        /// </summary>
        public string Status { get; set; } = "Completed";

        /// <summary>
        /// Gets or sets when the call was initiated.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the call ended, or null if it never connected.
        /// </summary>
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Gets or sets the connected call duration in seconds.
        /// </summary>
        public int DurationSeconds { get; set; }
    }

    /// <summary>
    /// Request body for recording a call outcome from the client that ended the call.
    /// </summary>
    public class RecordCallHistoryRequest
    {
        /// <summary>
        /// Gets or sets the call correlation ID shared by both peers during signaling.
        /// </summary>
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat this call belongs to.
        /// </summary>
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who initiated the call.
        /// </summary>
        public string CallerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who was called.
        /// </summary>
        public string CalleeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call type ("Audio" or "Video").
        /// </summary>
        public string CallType { get; set; } = "Audio";

        /// <summary>
        /// Gets or sets the call outcome status: Completed, Rejected, NoAnswer, or Failed.
        /// </summary>
        public string Status { get; set; } = "Completed";

        /// <summary>
        /// Gets or sets when the call was initiated.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the connected call duration in seconds.
        /// </summary>
        public int DurationSeconds { get; set; }
    }
}
