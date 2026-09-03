namespace NexusTeam.Shared.Dtos
{
    /// <summary>Reports one browser tab's visibility and call state.</summary>
    public class DeviceActivityRequest
    {
        /// <summary>Gets or sets the per-page tab identifier.</summary>
        public string TabId { get; set; } = string.Empty;

        /// <summary>Gets or sets a value indicating whether the tab is visible.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Gets or sets a value indicating whether the tab has an active call.</summary>
        public bool HasActiveCall { get; set; }
    }
}
