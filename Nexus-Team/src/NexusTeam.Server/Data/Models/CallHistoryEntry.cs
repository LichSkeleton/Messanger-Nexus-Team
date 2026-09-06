namespace NexusTeam.Server.Data.Models
{
    using System;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// MongoDB model for a completed/missed call record.
    /// </summary>
    public class CallHistoryEntry
    {
        /// <summary>
        /// Gets or sets the unique identifier for the history entry.
        /// </summary>
        [BsonId]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the call correlation ID shared by both peers during signaling.
        /// </summary>
        [BsonElement("callId")]
        public string CallId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat this call belongs to.
        /// </summary>
        [BsonElement("chatId")]
        public string ChatId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who initiated the call.
        /// </summary>
        [BsonElement("callerId")]
        public string CallerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user who was called.
        /// </summary>
        [BsonElement("calleeId")]
        public string CalleeId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the call type ("Audio" or "Video").
        /// </summary>
        [BsonElement("callType")]
        public string CallType { get; set; } = "Audio";

        /// <summary>
        /// Gets or sets the outcome: Completed, Rejected, Missed, NoAnswer, Failed.
        /// </summary>
        [BsonElement("status")]
        public string Status { get; set; } = "Completed";

        /// <summary>
        /// Gets or sets when the call was initiated.
        /// </summary>
        [BsonElement("startedAt")]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets when the call ended (null if never connected).
        /// </summary>
        [BsonElement("endedAt")]
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Gets or sets the connected call duration in seconds.
        /// </summary>
        [BsonElement("durationSeconds")]
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the callee has seen this entry
        /// (used to surface a "missed call" toast once, after reconnect).
        /// </summary>
        [BsonElement("seenByCallee")]
        public bool SeenByCallee { get; set; }
    }
}
