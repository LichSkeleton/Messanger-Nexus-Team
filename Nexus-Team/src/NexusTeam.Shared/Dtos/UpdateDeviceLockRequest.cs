namespace NexusTeam.Shared.Dtos
{
    /// <summary>Request used to update device-lock settings.</summary>
    public class UpdateDeviceLockRequest
    {
        /// <summary>Gets or sets the current PIN.</summary>
        public string CurrentPin { get; set; } = string.Empty;

        /// <summary>Gets or sets an optional replacement PIN.</summary>
        public string? NewPin { get; set; }

        /// <summary>Gets or sets the repeated replacement PIN.</summary>
        public string? ConfirmNewPin { get; set; }

        /// <summary>Gets or sets the inactivity timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; }
    }
}
