namespace NexusTeam.Shared.Dtos
{
    /// <summary>Describes the current device-lock state.</summary>
    public class DeviceLockStatusDto
    {
        /// <summary>Gets or sets a value indicating whether automatic locking is enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the inactivity timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 60;

        /// <summary>Gets or sets a value indicating whether the device is locked.</summary>
        public bool IsLocked { get; set; }

        /// <summary>Gets or sets a value indicating whether account sign-in is required.</summary>
        public bool RequiresPinReset { get; set; }

        /// <summary>Gets or sets the number of PIN attempts remaining.</summary>
        public int RemainingAttempts { get; set; } = 5;
    }
}
