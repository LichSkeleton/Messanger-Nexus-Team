namespace NexusTeam.Shared.Dtos
{
    /// <summary>Request used to enable automatic locking on the current device.</summary>
    public class EnableDeviceLockRequest
    {
        /// <summary>Gets or sets the account password used for reauthentication.</summary>
        public string AccountPassword { get; set; } = string.Empty;

        /// <summary>Gets or sets the new four-digit PIN.</summary>
        public string Pin { get; set; } = string.Empty;

        /// <summary>Gets or sets the repeated PIN.</summary>
        public string ConfirmPin { get; set; } = string.Empty;

        /// <summary>Gets or sets the inactivity timeout in seconds.</summary>
        public int TimeoutSeconds { get; set; } = 60;
    }
}
