namespace NexusTeam.Server.Data.Models
{
    using System;
    using System.Collections.Generic;
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>Persistent security and identity state for one user/browser device.</summary>
    public class UserDevice
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        [BsonElement("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [BsonElement("pinHash")]
        public string? PinHash { get; set; }

        [BsonElement("autoLockEnabled")]
        public bool AutoLockEnabled { get; set; }

        [BsonElement("lockTimeoutSeconds")]
        public int LockTimeoutSeconds { get; set; } = 60;

        [BsonElement("isLocked")]
        public bool IsLocked { get; set; }

        [BsonElement("inactiveSinceUtc")]
        public DateTime? InactiveSinceUtc { get; set; }

        [BsonElement("failedPinAttempts")]
        public int FailedPinAttempts { get; set; }

        [BsonElement("requiresPinReset")]
        public bool RequiresPinReset { get; set; }

        [BsonElement("revokedAtUtc")]
        public DateTime? RevokedAtUtc { get; set; }

        [BsonElement("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; }

        [BsonElement("lastSeenAtUtc")]
        public DateTime LastSeenAtUtc { get; set; }

        [BsonElement("updatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; }

        [BsonElement("visibleTabIds")]
        public List<string> VisibleTabIds { get; set; } = new List<string>();

        [BsonElement("activeCallTabIds")]
        public List<string> ActiveCallTabIds { get; set; } = new List<string>();
    }
}
