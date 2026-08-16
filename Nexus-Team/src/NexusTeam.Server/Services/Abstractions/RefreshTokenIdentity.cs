namespace NexusTeam.Server.Services.Abstractions
{
    public sealed class RefreshTokenIdentity
    {
        public RefreshTokenIdentity(string userId, string deviceId)
        {
            this.UserId = userId;
            this.DeviceId = deviceId;
        }

        public string UserId { get; }

        public string DeviceId { get; }
    }
}
