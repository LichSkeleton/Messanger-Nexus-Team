namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request to remove a reaction from a message.
    /// </summary>
    public class RemoveReactionRequest
    {
        /// <summary>
        /// Gets or sets the emoji of the reaction to remove.
        /// </summary>
        public string Emoji { get; set; } = string.Empty;
    }
}
