namespace NexusTeam.Client.Services
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NAudio.Wave;
    using NexusTeam.Client.Models;
    using NexusTeam.Shared.Contracts;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// Service for managing voice calls via NAudio and WebSocket.
    /// </summary>
    public class CallService : ICallService, IDisposable
    {
        private readonly IMessagingService messagingService;
        private readonly IAuthenticationService authenticationService;
        private readonly IUserDirectoryService userDirectoryService;
        private readonly ILogger logger;
        private readonly ServerConfiguration serverConfiguration;
        private string? currentCallId;
        private string? currentCallUserId;
        private CallState currentCallState = CallState.Idle;
        private bool isDisposed;
        private WaveInEvent? waveIn;
        private WaveOutEvent? waveOut;
        private BufferedWaveProvider? bufferedWaveProvider;
        private CancellationTokenSource? audioCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallService"/> class.
        /// </summary>
        /// <param name="messagingService">The messaging service for WebSocket communication.</param>
        /// <param name="authenticationService">The authentication service for current user context.</param>
        /// <param name="userDirectoryService">The user directory service for getting user names.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="serverConfiguration">Server configuration.</param>
        public CallService(
            IMessagingService messagingService,
            IAuthenticationService authenticationService,
            IUserDirectoryService userDirectoryService,
            ILogger logger,
            ServerConfiguration serverConfiguration)
        {
            this.messagingService = messagingService;
            this.authenticationService = authenticationService;
            this.userDirectoryService = userDirectoryService;
            this.logger = logger;
            this.serverConfiguration = serverConfiguration;

            // Subscribe to messaging service events to receive call messages
            this.messagingService.CallMessageReceived += this.OnCallMessageReceived;
        }

        /// <inheritdoc/>
        public bool IsInCall => this.currentCallId != null && this.currentCallState != CallState.Idle;

        /// <inheritdoc/>
        public string? CurrentCallId => this.currentCallId;

        /// <inheritdoc/>
        public string? CurrentCallUserId => this.currentCallUserId;

        /// <inheritdoc/>
        public CallState CurrentCallState => this.currentCallState;

        /// <inheritdoc/>
        public event EventHandler<IncomingCallEventArgs>? IncomingCall;

        /// <inheritdoc/>
        public event EventHandler<CallStateChangedEventArgs>? CallStateChanged;

        /// <inheritdoc/>
        public event EventHandler<string>? CallEnded;

        /// <inheritdoc/>
        public async Task StartCallAsync(string userId, string? chatId = null)
        {
            if (this.IsInCall)
            {
                this.logger.Warning("Cannot start call: already in a call");
                return;
            }

            var currentUserId = this.authenticationService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                this.logger.Warning("Cannot start call: user not authenticated");
                return;
            }

            var callId = Guid.NewGuid().ToString();
            this.currentCallId = callId;
            this.currentCallUserId = userId;
            this.SetCallState(CallState.Initiating);

            try
            {
                var callRequest = new CallRequestContract
                {
                    CallId = callId,
                    FromUserId = currentUserId,
                    ToUserId = userId,
                    ChatId = chatId,
                    Timestamp = DateTime.UtcNow,
                };

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.CallRequest,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(callRequest, options),
                };

                await this.messagingService.SendCallMessageAsync(envelope);
                this.logger.Information("Call request sent: {CallId} from {FromUserId} to {ToUserId}", callId, currentUserId, userId);
                this.SetCallState(CallState.Ringing);

                // Initialize WebRTC connection as offerer (will create offer when call is answered)
                // We wait for CallAnswer before creating offer
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to start call");
                this.CleanupCall();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task AnswerCallAsync(string callId)
        {
            if (this.currentCallId != callId)
            {
                this.logger.Warning("Cannot answer call: call ID mismatch");
                return;
            }

            var currentUserId = this.authenticationService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(this.currentCallUserId))
            {
                this.logger.Warning("Cannot answer call: invalid state");
                return;
            }

            try
            {
                var callAnswer = new CallAnswerContract
                {
                    CallId = callId,
                    FromUserId = currentUserId,
                    ToUserId = this.currentCallUserId,
                    Timestamp = DateTime.UtcNow,
                };

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.CallAnswer,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(callAnswer, options),
                };

                await this.messagingService.SendCallMessageAsync(envelope);
                this.logger.Information("Call answered: {CallId}", callId);
                this.SetCallState(CallState.Connecting);

                // Initialize audio connection as answerer
                await this.InitializeAudioAsAnswererAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to answer call");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task RejectCallAsync(string callId, string? reason = null)
        {
            var currentUserId = this.authenticationService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                this.logger.Warning("Cannot reject call: user not authenticated");
                return;
            }

            try
            {
                // Find the caller from current call or incoming call
                var toUserId = this.currentCallUserId;
                if (string.IsNullOrEmpty(toUserId))
                {
                    this.logger.Warning("Cannot reject call: no caller information");
                    return;
                }

                var callReject = new CallRejectContract
                {
                    CallId = callId,
                    FromUserId = currentUserId,
                    ToUserId = toUserId,
                    Reason = reason,
                    Timestamp = DateTime.UtcNow,
                };

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.CallReject,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(callReject, options),
                };

                await this.messagingService.SendCallMessageAsync(envelope);
                this.logger.Information("Call rejected: {CallId}, Reason: {Reason}", callId, reason ?? "No reason");
                this.CleanupCall();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to reject call");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task EndCallAsync(string callId)
        {
            if (this.currentCallId != callId)
            {
                this.logger.Warning("Cannot end call: call ID mismatch");
                return;
            }

            var currentUserId = this.authenticationService.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(this.currentCallUserId))
            {
                this.logger.Warning("Cannot end call: invalid state");
                return;
            }

            try
            {
                var callEnd = new CallEndContract
                {
                    CallId = callId,
                    FromUserId = currentUserId,
                    ToUserId = this.currentCallUserId,
                    Timestamp = DateTime.UtcNow,
                };

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.CallEnd,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(callEnd, options),
                };

                await this.messagingService.SendCallMessageAsync(envelope);
                this.logger.Information("Call ended: {CallId}", callId);
                this.SetCallState(CallState.Ending);

                // TODO: Cleanup WebRTC connection here
                this.CleanupCall();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to end call");
                this.CleanupCall();
                throw;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                if (this.messagingService != null)
                {
                    this.messagingService.CallMessageReceived -= this.OnCallMessageReceived;
                }

                this.CleanupCall();
                this.CleanupAudio();
                this.isDisposed = true;
            }
        }

        /// <summary>
        /// Handles incoming call request.
        /// </summary>
        /// <param name="callRequest">The call request contract.</param>
        internal async void HandleCallRequest(CallRequestContract callRequest)
        {
            this.logger.Information("HandleCallRequest called: CallId={CallId}, FromUserId={FromUserId}, ToUserId={ToUserId}", callRequest.CallId, callRequest.FromUserId, callRequest.ToUserId);

            if (this.IsInCall)
            {
                this.logger.Warning("Incoming call rejected: already in a call");

                // TODO: Send reject message
                return;
            }

            this.currentCallId = callRequest.CallId;
            this.currentCallUserId = callRequest.FromUserId;
            this.SetCallState(CallState.Ringing);

            this.logger.Information("Call state set to Ringing, raising IncomingCall event");

            // Get user name from directory
            string userName = "Unknown";
            try
            {
                var users = await this.userDirectoryService.GetAvailableUsersAsync();
                var user = users.FirstOrDefault(u => u.Id == callRequest.FromUserId);
                userName = user?.DisplayName ?? user?.Username ?? "Unknown";
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to get user name for: {UserId}", callRequest.FromUserId);
            }

            var args = new IncomingCallEventArgs
            {
                CallId = callRequest.CallId,
                FromUserId = callRequest.FromUserId,
                FromUserName = userName,
                ChatId = callRequest.ChatId,
            };

            this.IncomingCall?.Invoke(this, args);
            this.logger.Information("IncomingCall event raised for CallId={CallId}", callRequest.CallId);
        }

        /// <summary>
        /// Handles call answer.
        /// </summary>
        /// <param name="callAnswer">The call answer contract.</param>
        internal async void HandleCallAnswer(CallAnswerContract callAnswer)
        {
            if (this.currentCallId != callAnswer.CallId)
            {
                this.logger.Warning("Call answer received for different call: {CallId}", callAnswer.CallId);
                return;
            }

            this.logger.Information("Call answered by {UserId}", callAnswer.FromUserId);
            this.SetCallState(CallState.Connecting);

            // Initialize audio as offerer (call initiator)
            try
            {
                await this.InitializeAudioAsOffererAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to initialize audio after call answer");
            }
        }

        /// <summary>
        /// Handles call rejection.
        /// </summary>
        /// <param name="callReject">The call reject contract.</param>
        internal void HandleCallReject(CallRejectContract callReject)
        {
            if (this.currentCallId != callReject.CallId)
            {
                return;
            }

            this.logger.Information("Call rejected by {UserId}, Reason: {Reason}", callReject.FromUserId, callReject.Reason ?? "No reason");
            this.CleanupCall();
        }

        /// <summary>
        /// Handles call end.
        /// </summary>
        /// <param name="callEnd">The call end contract.</param>
        internal void HandleCallEnd(CallEndContract callEnd)
        {
            if (this.currentCallId != callEnd.CallId)
            {
                return;
            }

            this.logger.Information("Call ended by {UserId}", callEnd.FromUserId);
            this.CleanupCall();
        }

        /// <summary>
        /// Handles incoming audio data from remote peer.
        /// </summary>
        /// <param name="audioData">The audio data contract.</param>
        internal void HandleAudioData(CallAudioDataContract audioData)
        {
            if (this.currentCallId != audioData.CallId)
            {
                this.logger.Debug("HandleAudioData: Skipping - call ID mismatch. Expected: {Expected}, Got: {Got}", this.currentCallId, audioData.CallId);
                return;
            }

            try
            {
                if (this.bufferedWaveProvider == null)
                {
                    this.logger.Warning("HandleAudioData: BufferedWaveProvider is null, cannot play audio");
                    return;
                }

                // Convert base64 back to bytes
                var audioBytes = Convert.FromBase64String(audioData.AudioData);
                this.logger.Debug("HandleAudioData: Received {Bytes} bytes of audio data", audioBytes.Length);

                // Add to playback buffer
                this.bufferedWaveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
                this.logger.Debug(
                    "HandleAudioData: Added {Bytes} bytes to playback buffer. Buffer length: {BufferLength}, Buffered bytes: {BufferedBytes}",
                    audioBytes.Length,
                    this.bufferedWaveProvider.BufferLength,
                    this.bufferedWaveProvider.BufferedBytes);

                // Ensure playback is running
                if (this.waveOut != null && this.waveOut.PlaybackState != NAudio.Wave.PlaybackState.Playing)
                {
                    this.logger.Information("HandleAudioData: Restarting playback. Current state: {State}", this.waveOut.PlaybackState);
                    this.waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to process audio data");
            }
        }

        /// <summary>
        /// Handles incoming call messages from MessagingService.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="envelope">The WebSocket message envelope.</param>
        private void OnCallMessageReceived(object? sender, Shared.Dtos.WebSocketMessageEnvelope envelope)
        {
            this.logger.Information("OnCallMessageReceived: Type={Type}", envelope.Type);

            if (!envelope.Payload.HasValue)
            {
                this.logger.Warning("Received call message without payload");
                return;
            }

            try
            {
                var payloadText = envelope.Payload.Value.GetRawText();
                this.logger.Debug("Call message payload: {Payload}", payloadText);

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;

                switch (envelope.Type)
                {
                    case WebSocketMessageType.CallRequest:
                        var callRequest = System.Text.Json.JsonSerializer.Deserialize<CallRequestContract>(payloadText, options);
                        if (callRequest != null)
                        {
                            this.logger.Information("Deserialized CallRequest: CallId={CallId}, FromUserId={FromUserId}, ToUserId={ToUserId}", callRequest.CallId, callRequest.FromUserId, callRequest.ToUserId);
                            this.HandleCallRequest(callRequest);
                        }
                        else
                        {
                            this.logger.Warning("Failed to deserialize CallRequest - result is null");
                        }

                        break;

                    case WebSocketMessageType.CallAnswer:
                        var callAnswer = System.Text.Json.JsonSerializer.Deserialize<CallAnswerContract>(payloadText, options);
                        if (callAnswer != null)
                        {
                            this.HandleCallAnswer(callAnswer);
                        }

                        break;

                    case WebSocketMessageType.CallReject:
                        var callReject = System.Text.Json.JsonSerializer.Deserialize<CallRejectContract>(payloadText, options);
                        if (callReject != null)
                        {
                            this.HandleCallReject(callReject);
                        }

                        break;

                    case WebSocketMessageType.CallEnd:
                        var callEnd = System.Text.Json.JsonSerializer.Deserialize<CallEndContract>(payloadText, options);
                        if (callEnd != null)
                        {
                            this.HandleCallEnd(callEnd);
                        }

                        break;

                    case WebSocketMessageType.CallAudioData:
                        var audioData = System.Text.Json.JsonSerializer.Deserialize<CallAudioDataContract>(payloadText, options);
                        if (audioData != null)
                        {
                            this.HandleAudioData(audioData);
                        }

                        break;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                this.logger.Warning(ex, "Failed to deserialize call message: {Type}", envelope.Type);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error handling call message: {Type}", envelope.Type);
            }
        }

        private void SetCallState(CallState newState)
        {
            if (this.currentCallState == newState)
            {
                return;
            }

            var oldState = this.currentCallState;
            this.currentCallState = newState;

            this.logger.Debug("Call state changed: {OldState} -> {NewState}", oldState, newState);

            if (!string.IsNullOrEmpty(this.currentCallId))
            {
                var args = new CallStateChangedEventArgs
                {
                    CallId = this.currentCallId,
                    State = newState,
                };

                this.CallStateChanged?.Invoke(this, args);
            }
        }

        private void CleanupCall()
        {
            var callId = this.currentCallId;
            this.currentCallId = null;
            this.currentCallUserId = null;
            this.SetCallState(CallState.Idle);

            this.CleanupAudio();

            if (callId != null)
            {
                this.CallEnded?.Invoke(this, callId);
            }
        }

        /// <summary>
        /// Initializes audio connection as offerer (call initiator).
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeAudioAsOffererAsync()
        {
            try
            {
                this.logger.Information("Initializing audio as offerer");

                // Cleanup any existing connection
                this.CleanupAudio();

                // Setup audio format: 16kHz, 16-bit, mono
                var waveFormat = new WaveFormat(16000, 16, 1);

                // Initialize audio capture from microphone
                this.waveIn = new WaveInEvent
                {
                    WaveFormat = waveFormat,
                    BufferMilliseconds = 50, // 50ms buffers
                };

                this.waveIn.DataAvailable += this.OnAudioDataAvailable;
                this.waveIn.StartRecording();
                this.logger.Information("Audio capture started from microphone");

                // Initialize audio playback
                this.bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferLength = waveFormat.AverageBytesPerSecond * 2, // 2 seconds buffer
                    DiscardOnBufferOverflow = true,
                };

                this.waveOut = new WaveOutEvent();
                this.waveOut.Init(this.bufferedWaveProvider);
                this.waveOut.Play();
                this.logger.Information("Audio playback initialized");

                // Mark as connected
                await Task.Delay(100); // Small delay to ensure audio devices are ready
                this.SetCallState(CallState.Connected);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to initialize audio as offerer");
                this.CleanupAudio();
                throw;
            }
        }

        /// <summary>
        /// Initializes audio connection as answerer (call receiver).
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializeAudioAsAnswererAsync()
        {
            try
            {
                this.logger.Information("Initializing audio as answerer");

                // Cleanup any existing connection
                this.CleanupAudio();

                // Setup audio format: 16kHz, 16-bit, mono
                var waveFormat = new WaveFormat(16000, 16, 1);

                // Initialize audio capture from microphone
                this.waveIn = new WaveInEvent
                {
                    WaveFormat = waveFormat,
                    BufferMilliseconds = 50, // 50ms buffers
                };

                this.waveIn.DataAvailable += this.OnAudioDataAvailable;
                this.waveIn.StartRecording();
                this.logger.Information("Audio capture started from microphone");

                // Initialize audio playback
                this.bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
                {
                    BufferLength = waveFormat.AverageBytesPerSecond * 2, // 2 seconds buffer
                    DiscardOnBufferOverflow = true,
                };

                this.waveOut = new WaveOutEvent();
                this.waveOut.Init(this.bufferedWaveProvider);
                this.waveOut.Play();
                this.logger.Information("Audio playback initialized");

                // Mark as connected
                await Task.Delay(100); // Small delay to ensure audio devices are ready
                this.SetCallState(CallState.Connected);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to initialize audio as answerer");
                this.CleanupAudio();
                throw;
            }
        }

        /// <summary>
        /// Handles audio data available from microphone.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The wave in event args.</param>
        private async void OnAudioDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (string.IsNullOrEmpty(this.currentCallId) || string.IsNullOrEmpty(this.currentCallUserId))
            {
                this.logger.Debug("OnAudioDataAvailable: Skipping - no active call");
                return;
            }

            try
            {
                // Send audio data through WebSocket
                var currentUserId = this.authenticationService.CurrentUser?.Id;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    this.logger.Debug("OnAudioDataAvailable: Skipping - user not authenticated");
                    return;
                }

                this.logger.Debug("OnAudioDataAvailable: Captured {Bytes} bytes of audio", e.BytesRecorded);

                // Convert audio bytes to base64 for JSON transmission
                var audioData = Convert.ToBase64String(e.Buffer, 0, e.BytesRecorded);

                var audioContract = new CallAudioDataContract
                {
                    CallId = this.currentCallId,
                    FromUserId = currentUserId,
                    ToUserId = this.currentCallUserId,
                    AudioData = audioData,
                    Timestamp = DateTime.UtcNow,
                };

                var options = NexusTeam.Shared.Serialization.JsonSerializerOptionsFactory.WebSocket;
                var envelope = new WebSocketMessageEnvelope
                {
                    Type = WebSocketMessageType.CallAudioData,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(audioContract, options),
                };

                await this.messagingService.SendCallMessageAsync(envelope);
                this.logger.Debug("OnAudioDataAvailable: Sent audio data packet ({Size} bytes base64)", audioData.Length);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to send audio data");
            }
        }

        /// <summary>
        /// Cleans up audio resources.
        /// </summary>
        private void CleanupAudio()
        {
            try
            {
                if (this.waveIn != null)
                {
                    this.waveIn.StopRecording();
                    this.waveIn.DataAvailable -= this.OnAudioDataAvailable;
                    this.waveIn.Dispose();
                    this.waveIn = null;
                }

                if (this.waveOut != null)
                {
                    this.waveOut.Stop();
                    this.waveOut.Dispose();
                    this.waveOut = null;
                }

                this.bufferedWaveProvider = null;
                this.audioCancellationTokenSource?.Cancel();
                this.audioCancellationTokenSource = null;
                this.logger.Debug("Audio resources cleaned up");
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Error cleaning up audio resources");
            }
        }
    }
}
