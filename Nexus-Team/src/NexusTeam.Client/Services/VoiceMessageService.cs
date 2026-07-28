namespace NexusTeam.Client.Services
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NAudio.Wave;
    using Serilog;

    /// <summary>
    /// Service for recording voice messages.
    /// </summary>
    public class VoiceMessageService : IVoiceMessageService
    {
        private const int MinimumRecordingDurationMs = 500; // Minimum 500ms recording
        private readonly ILogger logger;
        private WaveInEvent? waveIn;
        private WaveFileWriter? waveFileWriter;
        private string? recordingFilePath;
        private bool isRecording;
        private bool isDisposed;
        private DateTime recordingStartTime;
        private bool hasRecordingStoppedEventBeenRaised;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceMessageService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public VoiceMessageService(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets a value indicating whether recording is currently in progress.
        /// </summary>
        public bool IsRecording => this.isRecording;

        /// <summary>
        /// Event raised when recording starts.
        /// </summary>
        public event EventHandler? RecordingStarted;

        /// <summary>
        /// Event raised when recording stops.
        /// </summary>
        public event EventHandler<string>? RecordingStopped;

        /// <summary>
        /// Starts recording audio to a temporary file.
        /// </summary>
        /// <returns>The path to the temporary audio file.</returns>
        public string StartRecording()
        {
            if (this.isRecording)
            {
                this.logger.Warning("Recording already in progress");
                return this.recordingFilePath ?? string.Empty;
            }

            try
            {
                // Setup audio format: 16kHz, 16-bit, mono (same as CallService)
                var waveFormat = new WaveFormat(16000, 16, 1);

                // Create temporary file for recording
                this.recordingFilePath = Path.Combine(
                    Path.GetTempPath(),
                    $"nexusteam_voice_{Guid.NewGuid()}.wav");

                // Initialize audio capture from microphone
                this.waveIn = new WaveInEvent
                {
                    WaveFormat = waveFormat,
                    BufferMilliseconds = 50, // 50ms buffers
                };

                this.waveIn.DataAvailable += this.OnAudioDataAvailable;
                this.waveIn.RecordingStopped += this.OnRecordingStopped;

                // Create WaveFileWriter to save audio to file
                this.waveFileWriter = new WaveFileWriter(this.recordingFilePath, waveFormat);

                // Start recording
                this.recordingStartTime = DateTime.Now;
                this.hasRecordingStoppedEventBeenRaised = false;
                this.waveIn.StartRecording();
                this.isRecording = true;

                this.logger.Information("Voice message recording started: {FilePath}", this.recordingFilePath);
                this.RecordingStarted?.Invoke(this, EventArgs.Empty);

                return this.recordingFilePath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to start voice message recording");
                this.Cleanup();
                throw;
            }
        }

        /// <summary>
        /// Stops recording and returns the path to the recorded file.
        /// </summary>
        /// <returns>The path to the recorded audio file, or null if recording failed.</returns>
        public string? StopRecording()
        {
            if (!this.isRecording)
            {
                this.logger.Warning("No recording in progress");
                return null;
            }

            try
            {
                this.waveIn?.StopRecording();

                // The file path will be returned in OnRecordingStopped event handler
                return this.recordingFilePath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to stop voice message recording");
                this.Cleanup();
                return null;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.Cleanup();

                // Clean up temporary file if it exists
                if (!string.IsNullOrEmpty(this.recordingFilePath) && File.Exists(this.recordingFilePath))
                {
                    try
                    {
                        File.Delete(this.recordingFilePath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Handles audio data available from microphone.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The wave in event args.</param>
        private void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (this.waveFileWriter != null && e.BytesRecorded > 0)
                {
                    this.waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    this.waveFileWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Error writing audio data to file");
            }
        }

        /// <summary>
        /// Handles recording stopped event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The stopped event args.</param>
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            // Prevent multiple invocations of this event handler
            if (this.hasRecordingStoppedEventBeenRaised)
            {
                this.logger.Warning("RecordingStopped event already raised, ignoring duplicate call");
                return;
            }

            this.hasRecordingStoppedEventBeenRaised = true;

            try
            {
                var filePath = this.recordingFilePath;

                // Dispose WaveFileWriter to finalize the WAV file
                if (this.waveFileWriter != null)
                {
                    this.waveFileWriter.Dispose();
                    this.waveFileWriter = null;
                }

                this.isRecording = false;

                // Check if file exists and has content
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    var recordingDuration = (DateTime.Now - this.recordingStartTime).TotalMilliseconds;

                    // Check minimum duration and file size
                    if (fileInfo.Length > 0 && recordingDuration >= MinimumRecordingDurationMs)
                    {
                        this.logger.Information(
                            "Voice message recording stopped: {FilePath}, Size: {Size} bytes, Duration: {Duration}ms",
                            filePath,
                            fileInfo.Length,
                            recordingDuration);
                        this.RecordingStopped?.Invoke(this, filePath);
                    }
                    else
                    {
                        this.logger.Warning(
                            "Recorded file is too short or empty: {FilePath}, Size: {Size} bytes, Duration: {Duration}ms",
                            filePath,
                            fileInfo.Length,
                            recordingDuration);
                        try
                        {
                            File.Delete(filePath);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }

                        this.RecordingStopped?.Invoke(this, string.Empty);
                    }
                }
                else
                {
                    this.logger.Warning("Recorded file does not exist: {FilePath}", filePath);
                    this.RecordingStopped?.Invoke(this, string.Empty);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error in recording stopped handler");
                this.RecordingStopped?.Invoke(this, string.Empty);
            }
            finally
            {
                this.Cleanup();
            }
        }

        /// <summary>
        /// Cleans up recording resources.
        /// </summary>
        private void Cleanup()
        {
            try
            {
                if (this.waveIn != null)
                {
                    this.waveIn.DataAvailable -= this.OnAudioDataAvailable;
                    this.waveIn.RecordingStopped -= this.OnRecordingStopped;
                    this.waveIn.Dispose();
                    this.waveIn = null;
                }

                if (this.waveFileWriter != null)
                {
                    this.waveFileWriter.Dispose();
                    this.waveFileWriter = null;
                }

                this.isRecording = false;
                this.hasRecordingStoppedEventBeenRaised = false;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Error cleaning up recording resources");
            }
        }
    }
}
