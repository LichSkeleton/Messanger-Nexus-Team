namespace NexusTeam.Server.Services.Abstractions
{
    /// <summary>
    /// Identifies the user and browser device carried by an access token.
    /// </summary>
    public sealed class AuthenticatedIdentity
    {
        public AuthenticatedIdentity(string userId, string? deviceId)
        {
            this.UserId = userId;
            this.DeviceId = deviceId;
        }

        public string UserId { get; }

        public string? DeviceId { get; }
    }
}
