namespace NexusTeam.Shared.Dtos
{
    using System;

    /// <summary>
    /// DTO representing a call history entry returned to clients.
    /// </summary>
    public class CallHistoryDto
    {
        public string Id { get; set; } = string.Empty;

        public string CallId { get; set; } = string.Empty;

        public string ChatId { get; set; } = string.Empty;

        public string CallerId { get; set; } = string.Empty;

        public string CalleeId { get; set; } = string.Empty;

        public string CallType { get; set; } = "Audio";

        public string Status { get; set; } = "Completed";

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public int DurationSeconds { get; set; }
    }

    /// <summary>
    /// Request body for recording a call outcome from the client that ended the call.
    /// </summary>
    public class RecordCallHistoryRequest
    {
        public string CallId { get; set; } = string.Empty;

        public string ChatId { get; set; } = string.Empty;

        public string CallerId { get; set; } = string.Empty;

        public string CalleeId { get; set; } = string.Empty;

        public string CallType { get; set; } = "Audio";

        /// <summary>Completed, Rejected, NoAnswer, Failed.</summary>
        public string Status { get; set; } = "Completed";

        public DateTime StartedAt { get; set; }

        public int DurationSeconds { get; set; }
    }
}
