namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using Serilog;

    /// <summary>
    /// View model for managing voice calls.
    /// </summary>
    public partial class CallViewModel : ViewModelBase
    {
        private readonly ICallService callService;
        private readonly IUserDirectoryService userDirectoryService;
        private readonly IAvatarService avatarService;
        private readonly ILogger logger;
        private readonly DispatcherTimer callTimer;
        private DateTime? callConnectedAt;
        private string callDurationText = "00:00";
        private string? callerName;
        private string? callerId;
        private string? currentCallId;
        private CallState callState = CallState.Idle;
        private bool isIncomingCall;
        private System.Windows.Media.Imaging.BitmapImage? callerAvatar;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallViewModel"/> class.
        /// </summary>
        /// <param name="callService">The call service.</param>
        /// <param name="userDirectoryService">The user directory service.</param>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="logger">The logger instance.</param>
        public CallViewModel(
            ICallService callService,
            IUserDirectoryService userDirectoryService,
            IAvatarService avatarService,
            ILogger logger)
        {
            this.callService = callService ?? throw new ArgumentNullException(nameof(callService));
            this.userDirectoryService = userDirectoryService ?? throw new ArgumentNullException(nameof(userDirectoryService));
            this.avatarService = avatarService ?? throw new ArgumentNullException(nameof(avatarService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to call service events
            this.callService.IncomingCall += this.OnIncomingCall;
            this.callService.CallStateChanged += this.OnCallStateChanged;
            this.callService.CallEnded += this.OnCallEnded;

            // Initialize from current call state if any
            this.UpdateCallState();

            this.callTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            this.callTimer.Tick += this.OnCallTimerTick;
        }

        /// <summary>
        /// Gets a value indicating whether there is an active call.
        /// </summary>
        public bool IsCallActive => this.callState != CallState.Idle;

        /// <summary>
        /// Gets the current call state.
        /// </summary>
        public CallState CallState
        {
            get => this.callState;
            private set
            {
                if (this.SetProperty(ref this.callState, value))
                {
                    this.OnPropertyChanged(nameof(this.IsCallActive));
                    this.OnPropertyChanged(nameof(this.IsRinging));
                    this.OnPropertyChanged(nameof(this.IsConnecting));
                    this.OnPropertyChanged(nameof(this.IsConnected));
                    this.OnPropertyChanged(nameof(this.IsOutgoingCall));
                    this.OnPropertyChanged(nameof(this.CanAnswer));
                    this.OnPropertyChanged(nameof(this.CanReject));
                    this.OnPropertyChanged(nameof(this.CanEnd));
                    this.NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the call is ringing.
        /// </summary>
        public bool IsRinging => this.callState == CallState.Ringing;

        /// <summary>
        /// Gets a value indicating whether the call is connecting.
        /// </summary>
        public bool IsConnecting => this.callState == CallState.Connecting;

        /// <summary>
        /// Gets a value indicating whether the call is connected.
        /// </summary>
        public bool IsConnected => this.callState == CallState.Connected;

        /// <summary>
        /// Gets the formatted call duration.
        /// </summary>
        public string CallDurationText
        {
            get => this.callDurationText;
            private set => this.SetProperty(ref this.callDurationText, value);
        }

        /// <summary>
        /// Gets a value indicating whether call duration has started (connected at least once).
        /// </summary>
        public bool HasCallDuration => this.callConnectedAt.HasValue;

        /// <summary>
        /// Gets the name of the caller or callee.
        /// </summary>
        public string? CallerName
        {
            get => this.callerName;
            private set => this.SetProperty(ref this.callerName, value);
        }

        /// <summary>
        /// Gets the ID of the caller or callee.
        /// </summary>
        public string? CallerId
        {
            get => this.callerId;
            private set
            {
                if (this.SetProperty(ref this.callerId, value))
                {
                    _ = this.LoadCallerAvatarAsync();
                }
            }
        }

        /// <summary>
        /// Gets the avatar image of the caller or callee.
        /// </summary>
        public System.Windows.Media.Imaging.BitmapImage? CallerAvatar
        {
            get => this.callerAvatar;
            private set => this.SetProperty(ref this.callerAvatar, value);
        }

        /// <summary>
        /// Gets the current call identifier.
        /// </summary>
        public string? CurrentCallId
        {
            get => this.currentCallId;
            private set
            {
                if (this.SetProperty(ref this.currentCallId, value))
                {
                    this.OnPropertyChanged(nameof(this.CanAnswer));
                    this.OnPropertyChanged(nameof(this.CanReject));
                    this.OnPropertyChanged(nameof(this.CanEnd));
                    this.NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an incoming call.
        /// </summary>
        public bool IsIncomingCall
        {
            get => this.isIncomingCall;
            private set
            {
                if (this.SetProperty(ref this.isIncomingCall, value))
                {
                    this.OnPropertyChanged(nameof(this.IsOutgoingCall));
                    this.OnPropertyChanged(nameof(this.CanAnswer));
                    this.OnPropertyChanged(nameof(this.CanReject));
                    this.NotifyCommandsCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this is an outgoing call.
        /// </summary>
        public bool IsOutgoingCall => this.IsCallActive && !this.isIncomingCall;

        /// <summary>
        /// Gets a value indicating whether the call can be answered.
        /// </summary>
        public bool CanAnswer
        {
            get
            {
                var canAnswer = this.IsRinging && this.isIncomingCall && !string.IsNullOrEmpty(this.currentCallId);
                this.logger.Debug(
                    "CanAnswer check: IsRinging={IsRinging}, IsIncomingCall={IsIncomingCall}, HasCallId={HasCallId}, Result={Result}",
                    this.IsRinging,
                    this.isIncomingCall,
                    !string.IsNullOrEmpty(this.currentCallId),
                    canAnswer);
                return canAnswer;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the call can be rejected.
        /// </summary>
        public bool CanReject => this.IsRinging && this.isIncomingCall && !string.IsNullOrEmpty(this.currentCallId);

        /// <summary>
        /// Gets a value indicating whether the call can be ended.
        /// </summary>
        public bool CanEnd => (this.IsConnected || this.IsConnecting || this.IsOutgoingCall) && !string.IsNullOrEmpty(this.currentCallId);

        /// <summary>
        /// Occurs when the call state changes.
        /// </summary>
        public event EventHandler<Services.CallStateChangedEventArgs>? CallStateChanged;

        /// <summary>
        /// Starts a call to the specified user.
        /// </summary>
        /// <param name="userId">The user ID to call.</param>
        /// <param name="chatId">Optional chat ID if call is from a chat.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StartCallAsync(string userId, string? chatId = null)
        {
            if (this.IsCallActive)
            {
                this.logger.Warning("Cannot start call: already in a call");
                return;
            }

            try
            {
                // Get user name from directory
                var userName = await this.GetUserNameFromDirectoryAsync(userId);
                this.CallerId = userId;
                this.CallerName = userName ?? "Unknown User";
                this.IsIncomingCall = false;

                await this.callService.StartCallAsync(userId, chatId);

                // Sync call ID and state from service after starting call
                this.CurrentCallId = this.callService.CurrentCallId;
                this.CallState = this.callService.CurrentCallState;

                this.logger.Information("Call started to: {UserId}, CallId: {CallId}", userId, this.CurrentCallId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to start call to: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Called when the view model is navigated away from.
        /// </summary>
        public override void OnNavigatedFrom()
        {
            // Unsubscribe from events
            if (this.callService != null)
            {
                this.callService.IncomingCall -= this.OnIncomingCall;
                this.callService.CallStateChanged -= this.OnCallStateChanged;
                this.callService.CallEnded -= this.OnCallEnded;
            }
        }

        /// <summary>
        /// Gets the command to answer an incoming call.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanAnswer))]
        private async Task AnswerCallAsync()
        {
            this.logger.Information("AnswerCallAsync called: CallId={CallId}, CanAnswer={CanAnswer}", this.currentCallId, this.CanAnswer);

            if (string.IsNullOrEmpty(this.currentCallId))
            {
                this.logger.Warning("Cannot answer call: call ID is null or empty");
                return;
            }

            if (!this.CanAnswer)
            {
                this.logger.Warning("Cannot answer call: CanAnswer is false");
                return;
            }

            try
            {
                this.logger.Information("Calling callService.AnswerCallAsync for call {CallId}", this.currentCallId);
                await this.callService.AnswerCallAsync(this.currentCallId);
                this.logger.Information("Call answered successfully: {CallId}", this.currentCallId);

                // Update state after answering
                this.CallState = this.callService.CurrentCallState;
                this.OnPropertyChanged(nameof(this.CanAnswer));
                this.OnPropertyChanged(nameof(this.CanReject));
                this.OnPropertyChanged(nameof(this.CanEnd));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("mrwebrtc") || ex.Message.Contains("Visual C++"))
            {
                this.logger.Error(ex, "Failed to answer call due to missing WebRTC dependencies: {CallId}", this.currentCallId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        ex.Message,
                        "WebRTC Library Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to answer call: {CallId}", this.currentCallId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Failed to answer call: {ex.Message}",
                        "Call Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Gets the command to reject an incoming call.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanReject))]
        private async Task RejectCallAsync()
        {
            this.logger.Information("RejectCallAsync called: CallId={CallId}, CanReject={CanReject}", this.currentCallId, this.CanReject);

            if (string.IsNullOrEmpty(this.currentCallId))
            {
                this.logger.Warning("Cannot reject call: call ID is null or empty");
                return;
            }

            if (!this.CanReject)
            {
                this.logger.Warning("Cannot reject call: CanReject is false");
                return;
            }

            try
            {
                this.logger.Information("Calling callService.RejectCallAsync for call {CallId}", this.currentCallId);
                await this.callService.RejectCallAsync(this.currentCallId);
                this.logger.Information("Call rejected successfully: {CallId}", this.currentCallId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to reject call: {CallId}", this.currentCallId);
            }
        }

        /// <summary>
        /// Gets the command to end the current call.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEnd))]
        private async Task EndCallAsync()
        {
            if (string.IsNullOrEmpty(this.currentCallId))
            {
                this.logger.Warning("Cannot end call: call ID is null or empty");
                return;
            }

            try
            {
                await this.callService.EndCallAsync(this.currentCallId);
                this.logger.Information("Call ended: {CallId}", this.currentCallId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to end call: {CallId}", this.currentCallId);
            }
        }

        /// <summary>
        /// Handles incoming call event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnIncomingCall(object? sender, IncomingCallEventArgs e)
        {
            this.logger.Information("OnIncomingCall received: CallId={CallId}, FromUserId={FromUserId}, FromUserName={FromUserName}", e.CallId, e.FromUserId, e.FromUserName);

            // Ensure we're on the UI thread before updating UI-bound properties
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => this.OnIncomingCall(sender, e));
                return;
            }

            this.CurrentCallId = e.CallId;
            this.CallerId = e.FromUserId;
            this.CallerName = e.FromUserName;
            this.IsIncomingCall = true;
            this.CallDurationText = "00:00";

            // Update call state from service (should be Ringing at this point)
            // This ensures the UI shows the call window
            var serviceState = this.callService.CurrentCallState;
            this.logger.Information("Service call state: {ServiceState}", serviceState);

            // Always set to Ringing for incoming calls, even if service state is different
            if (serviceState == CallState.Idle || serviceState == CallState.Ringing)
            {
                this.CallState = CallState.Ringing;
            }
            else
            {
                this.CallState = serviceState;
            }

            // Ensure CanAnswer and CanReject are updated
            this.OnPropertyChanged(nameof(this.CanAnswer));
            this.OnPropertyChanged(nameof(this.CanReject));
            this.OnPropertyChanged(nameof(this.CanEnd));
            this.NotifyCommandsCanExecuteChanged();

            this.logger.Information("CallViewModel state set to: {State}, CanAnswer={CanAnswer}, CanReject={CanReject}", this.CallState, this.CanAnswer, this.CanReject);

            // Always raise CallStateChanged event to show the call window for incoming calls
            var args = new Services.CallStateChangedEventArgs
            {
                CallId = e.CallId,
                State = this.CallState,
            };

            this.logger.Information("Raising CallStateChanged event: CallId={CallId}, State={State}", args.CallId, args.State);
            this.CallStateChanged?.Invoke(this, args);

            this.logger.Information("Incoming call processed: FromUserId={FromUserId}, FromUserName={FromUserName}, State={State}", e.FromUserId, e.FromUserName, this.CallState);
        }

        /// <summary>
        /// Handles call state changed event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCallStateChanged(object? sender, CallStateChangedEventArgs e)
        {
            // Ensure we're on the UI thread before updating UI-bound properties
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => this.OnCallStateChanged(sender, e));
                return;
            }

            // Accept state changes for current call or if we don't have a call ID yet (for outgoing calls)
            if (string.IsNullOrEmpty(this.currentCallId) || e.CallId == this.currentCallId)
            {
                this.CallState = e.State;
                this.CurrentCallId = e.CallId;

                if (e.State == CallState.Connected)
                {
                    this.callConnectedAt = DateTime.UtcNow;
                    this.OnPropertyChanged(nameof(this.HasCallDuration));
                    this.CallDurationText = "00:00";
                    this.callTimer.Start();
                }
                else if (e.State == CallState.Ending || e.State == CallState.Idle)
                {
                    this.callConnectedAt = null;
                    this.OnPropertyChanged(nameof(this.HasCallDuration));
                    this.callTimer.Stop();
                }

                // Update caller info if needed
                if (this.callService.CurrentCallUserId != null && this.CallerId != this.callService.CurrentCallUserId)
                {
                    this.CallerId = this.callService.CurrentCallUserId;
                    _ = this.UpdateCallerNameAsync();
                }

                this.logger.Debug("Call state changed: {State} for call {CallId}", e.State, e.CallId);

                // Raise event for UI
                this.CallStateChanged?.Invoke(this, e);
            }
            else
            {
                this.logger.Debug("Ignoring call state change for different call: {CallId} (current: {CurrentCallId})", e.CallId, this.currentCallId);
            }
        }

        /// <summary>
        /// Handles call ended event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="callId">The call ID.</param>
        private void OnCallEnded(object? sender, string callId)
        {
            // Ensure we're on the UI thread before updating UI-bound properties
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.Invoke(() => this.OnCallEnded(sender, callId));
                return;
            }

            if (callId == this.currentCallId)
            {
                this.logger.Information("Call ended: {CallId}", callId);
                this.ResetCallState();
                this.callTimer.Stop();
                this.CallDurationText = "00:00";

                // Explicitly raise CallStateChanged event to ensure window closes
                var args = new Services.CallStateChangedEventArgs
                {
                    CallId = callId,
                    State = CallState.Idle,
                };
                this.CallStateChanged?.Invoke(this, args);
            }
        }

        /// <summary>
        /// Updates the call state from the call service.
        /// </summary>
        private void UpdateCallState()
        {
            if (this.callService.IsInCall)
            {
                this.CurrentCallId = this.callService.CurrentCallId;
                this.CallerId = this.callService.CurrentCallUserId;
                this.CallState = this.callService.CurrentCallState;
                this.IsIncomingCall = false; // Will be updated when we know the direction

                _ = this.UpdateCallerNameAsync();
            }
        }

        /// <summary>
        /// Loads the caller avatar asynchronously.
        /// </summary>
        private async Task LoadCallerAvatarAsync()
        {
            if (this.avatarService == null || string.IsNullOrEmpty(this.callerId))
            {
                this.CallerAvatar = this.avatarService?.GetDefaultAvatar();
                return;
            }

            try
            {
                var users = await this.userDirectoryService.GetAvailableUsersAsync();
                var user = users.FirstOrDefault(u => u.Id == this.callerId);

                if (user != null && !string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var separator = user.AvatarUrl.Contains('?') ? "&" : "?";
                    var cacheBustingUrl = $"{user.AvatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                    this.CallerAvatar = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
                }
                else
                {
                    this.CallerAvatar = this.avatarService.GetDefaultAvatar();
                }
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load caller avatar");
                this.CallerAvatar = this.avatarService.GetDefaultAvatar();
            }
        }

        /// <summary>
        /// Gets user name from directory by user ID.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The user name or null if not found.</returns>
        private async Task<string?> GetUserNameFromDirectoryAsync(string userId)
        {
            try
            {
                var users = await this.userDirectoryService.GetAvailableUsersAsync();
                var user = users.FirstOrDefault(u => u.Id == userId);
                return user?.DisplayName ?? user?.Username;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to get user name for: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Updates the caller name from the user directory.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateCallerNameAsync()
        {
            if (string.IsNullOrEmpty(this.CallerId))
            {
                return;
            }

            try
            {
                var userName = await this.GetUserNameFromDirectoryAsync(this.CallerId);
                this.CallerName = userName ?? "Unknown User";
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to get user name for: {UserId}", this.CallerId);
                this.CallerName = "Unknown User";
            }
        }

        /// <summary>
        /// Resets the call state.
        /// </summary>
        private void ResetCallState()
        {
            this.CurrentCallId = null;
            this.CallerId = null;
            this.CallerName = null;
            this.CallState = CallState.Idle;
            this.IsIncomingCall = false;
            this.callTimer.Stop();
            this.CallDurationText = "00:00";
        }

        private void OnCallTimerTick(object? sender, EventArgs e)
        {
            if (this.callConnectedAt == null)
            {
                this.CallDurationText = "00:00";
                return;
            }

            var elapsed = DateTime.UtcNow - this.callConnectedAt.Value;
            if (elapsed.TotalHours >= 1)
            {
                this.CallDurationText = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
            else
            {
                this.CallDurationText = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
        }

        /// <summary>
        /// Notifies all commands that their CanExecute status may have changed.
        /// This method safely handles thread marshaling to the UI thread.
        /// </summary>
        private void NotifyCommandsCanExecuteChanged()
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null)
            {
                if (dispatcher.CheckAccess())
                {
                    // Already on UI thread
                    this.AnswerCallCommand.NotifyCanExecuteChanged();
                    this.RejectCallCommand.NotifyCanExecuteChanged();
                    this.EndCallCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    // Need to marshal to UI thread
                    dispatcher.Invoke(() =>
                    {
                        this.AnswerCallCommand.NotifyCanExecuteChanged();
                        this.RejectCallCommand.NotifyCanExecuteChanged();
                        this.EndCallCommand.NotifyCanExecuteChanged();
                    });
                }
            }
            else
            {
                // Fallback if dispatcher is not available (shouldn't happen in WPF)
                this.AnswerCallCommand.NotifyCanExecuteChanged();
                this.RejectCallCommand.NotifyCanExecuteChanged();
                this.EndCallCommand.NotifyCanExecuteChanged();
            }
        }
    }
}
