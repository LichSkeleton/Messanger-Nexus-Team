namespace NexusTeam.Shared.Dtos
{
    /// <summary>
    /// Request to add a reaction to a message.
    /// </summary>
    public class AddReactionRequest
    {
        /// <summary>
        /// Gets or sets the emoji for the reaction.
        /// </summary>
        public string Emoji { get; set; } = string.Empty;
    }
}
