namespace NexusTeam.Shared.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Type of call being initiated.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<CallType>))]
    public enum CallType
    {
        /// <summary>
        /// Audio-only call.
        /// </summary>
        Audio,

        /// <summary>
        /// Audio and video call.
        /// </summary>
        Video,
    }
}
