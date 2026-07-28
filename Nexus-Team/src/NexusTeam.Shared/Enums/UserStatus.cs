namespace NexusTeam.Shared.Enums
{
    /// <summary>
    /// Represents the online/offline status of a user.
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// User is offline.
        /// </summary>
        Offline = 0,

        /// <summary>
        /// User is online and available.
        /// </summary>
        Online = 1,

        /// <summary>
        /// User is away from keyboard.
        /// </summary>
        Away = 2,

        /// <summary>
        /// User is in do not disturb mode.
        /// </summary>
        DoNotDisturb = 3,

        /// <summary>
        /// User is connected but appears offline to others.
        /// </summary>
        Invisible = 4,
    }
}
