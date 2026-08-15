namespace NexusTeam.Shared.Dtos
{
    /// <summary>Request containing a device PIN.</summary>
    public class VerifyDevicePinRequest
    {
        /// <summary>Gets or sets the four-digit PIN.</summary>
        public string Pin { get; set; } = string.Empty;
    }
}
