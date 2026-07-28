namespace NexusTeam.Client.Services
{
    using System;

    /// <summary>
    /// Interface for recording voice messages.
    /// </summary>
    public interface IVoiceMessageService : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether recording is currently in progress.
        /// </summary>
        bool IsRecording { get; }

        /// <summary>
        /// Event raised when recording starts.
        /// </summary>
        event EventHandler? RecordingStarted;

        /// <summary>
        /// Event raised when recording stops.
        /// </summary>
        event EventHandler<string>? RecordingStopped;

        /// <summary>
        /// Starts recording audio to a temporary file.
        /// </summary>
        /// <returns>The path to the temporary audio file.</returns>
        string StartRecording();

        /// <summary>
        /// Stops recording and returns the path to the recorded file.
        /// </summary>
        /// <returns>The path to the recorded audio file, or null if recording failed.</returns>
        string? StopRecording();
    }
}
