# NexusTeam Client - Complete Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Project Structure](#project-structure)
4. [Views](#views)
5. [ViewModels](#viewmodels)
6. [Services](#services)
7. [Infrastructure](#infrastructure)
8. [Themes and Styling](#themes-and-styling)
9. [Communication](#communication)
10. [Local Storage](#local-storage)
11. [Error Handling](#error-handling)
12. [Logging](#logging)
13. [Build and Run](#build-and-run)
14. [Configuration](#configuration)

---

## Overview

> **Note:** The primary, cross-platform client is the responsive web SPA in `Nexus-Team/web/` (open `http://localhost:8080`). This document describes the **optional Windows-only WPF client**.

NexusTeam.Client is a WPF (Windows Presentation Foundation) desktop application built with .NET 8.0. It implements the MVVM (Model-View-ViewModel) pattern and provides a modern desktop UI with real-time messaging and WebRTC voice calls. Use it when you need Windows-native features; otherwise prefer the web client.

### Key Features

- **MVVM Architecture**: Clean separation of concerns using CommunityToolkit.Mvvm
- **Real-Time Messaging**: WebSocket client with automatic reconnection
- **Voice Calls**: WebRTC-based voice calling with NAudio for audio capture/playback
- **Modern UI**: Beautiful interface with theme support (light/dark)
- **File Attachments**: Upload and manage file attachments
- **Image Generation**: AI-powered image generation interface
- **Code Preview**: Syntax highlighting for code snippets
- **Translation**: Built-in message translation
- **Offline Support**: Offline message queue
- **Session Management**: Automatic session restoration
- **Error Handling**: Comprehensive error handling and user feedback

### Technology Stack

- **Framework**: WPF (.NET 8.0-windows)
- **MVVM**: CommunityToolkit.Mvvm
- **WebSocket**: System.Net.WebSockets.Client
- **HTTP Client**: Microsoft.Extensions.Http
- **Audio**: NAudio (voice calls)
- **Logging**: Serilog
- **Local Storage**: LiteDB
- **Code Editor**: AvalonEdit
- **Dependency Injection**: Microsoft.Extensions.Hosting

---

## Architecture

### MVVM Pattern

```
┌─────────────────────────────────────────────┐
│              Views (XAML)                    │
│  - UI Definition                            │
│  - Data Binding                             │
│  - User Interaction                         │
└──────────────────┬──────────────────────────┘
                   │ Data Binding
┌──────────────────▼──────────────────────────┐
│           ViewModels                         │
│  - Business Logic                            │
│  - State Management                          │
│  - Command Handling                          │
└──────────────────┬──────────────────────────┘
                   │ Service Calls
┌──────────────────▼──────────────────────────┐
│              Services                        │
│  - API Communication                        │
│  - WebSocket Client                          │
│  - Local Storage                             │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Server / Databases                    │
└──────────────────────────────────────────────┘
```

### Dependency Injection

The application uses `Microsoft.Extensions.Hosting` for dependency injection:

- Services registered in `Infrastructure/DependencyInjection.cs`
- ViewModels and Views registered as services
- Services resolved through `IServiceProvider`
- Lifetime management (Singleton, Scoped, Transient)

---

## Project Structure

```
NexusTeam.Client/
├── Assets/                  # Application assets
│   └── app.ico             # Application icon
├── Converters/             # Value converters (16)
├── Helpers/                # Helper classes
│   ├── CodeLanguageDetector.cs
│   ├── DateFormatter.cs
│   └── RichTextBoxHelper.cs
├── Infrastructure/         # Core infrastructure
│   ├── DependencyInjection.cs
│   ├── IMessageBus.cs
│   ├── MessageBus.cs
│   ├── Messages/
│   └── RelayCommand.cs
├── Models/                 # Data models
│   └── ServerConfiguration.cs
├── Selectors/              # Data template selectors
│   └── MessageItemTemplateSelector.cs
├── Services/               # Application services (15)
├── Themes/                 # Theme resources
│   ├── Converters.xaml
│   ├── DarkTheme.xaml
│   └── Styles.xaml
├── ViewModels/             # View models (21)
├── Views/                  # WPF views (16 XAML + code-behind)
├── App.xaml                # Application definition
├── App.xaml.cs             # Application entry point
└── appsettings.json        # Configuration
```

---

## Views

### Main Views

#### 1. MainWindow
**Purpose**: Main application window with navigation

**Features**:
- Window chrome (custom title bar)
- Navigation container
- Window controls (minimize, maximize, close)
- Resizable and draggable

#### 2. WelcomeView
**Purpose**: Welcome/login screen

**Features**:
- Application branding
- Login button
- Register button
- Session restoration

#### 3. LoginView
**Purpose**: User authentication

**Features**:
- Username/email input
- Password input
- Remember me checkbox
- Login button
- Link to registration

#### 4. RegisterView
**Purpose**: User registration

**Features**:
- Username input
- Email input
- Password input
- Display name input
- Registration button
- Link to login

#### 5. ChatView
**Purpose**: Main chat interface

**Features**:
- Chat list sidebar
- Conversation view
- Message input
- User status indicators
- Search functionality
- Settings access
- Start call button

#### 6. ConversationView
**Purpose**: Message conversation display

**Features**:
- Message list with virtualization
- Message input area
- File attachment button
- Emoji picker
- Message status indicators
- Reply functionality
- Message editing
- Message deletion

### Dialog Views

#### 7. CreateChatDialog
**Purpose**: Create new chat

**Features**:
- Chat type selection (direct, group, channel)
- User selection
- Chat name input
- Create button

#### 8. CreateFolderDialog
**Purpose**: Create chat folder

**Features**:
- Folder name input
- Create button

#### 9. SettingsView
**Purpose**: Application settings

**Features**:
- Theme selection
- Language selection
- Notification settings
- Privacy settings
- Account management

### Utility Views

#### 10. CodePreviewWindow
**Purpose**: Code snippet preview

**Features**:
- Syntax highlighting (AvalonEdit)
- Language detection
- Copy to clipboard
- Multiple language support

#### 11. TranslateWindow
**Purpose**: Message translation

**Features**:
- Source language selection
- Target language selection
- Translation display
- Copy functionality

#### 12. FilesListView
**Purpose**: File management

**Features**:
- File list display
- File download
- File preview
- File deletion

#### 13. ImagesGridView
**Purpose**: Generated images gallery

**Features**:
- Image grid display
- Image preview
- Image download
- Image deletion

#### 14. GeneratorView
**Purpose**: AI image generation

**Features**:
- Prompt input
- Generation parameters
- Generate button
- Generated image display

---

## ViewModels

### Main ViewModels

#### 1. MainWindowViewModel
**Purpose**: Application state and navigation

**Properties**:
- CurrentViewModel (navigation target)
- Title
- IsBusy

**Commands**:
- NavigateToCommand
- NavigateBackCommand
- CloseCommand
- MinimizeCommand
- MaximizeCommand

#### 2. WelcomeViewModel
**Purpose**: Welcome screen logic

**Commands**:
- NavigateToLoginCommand
- NavigateToRegisterCommand

#### 3. LoginViewModel
**Purpose**: Login/authentication logic

**Properties**:
- UsernameOrEmail
- Password
- RememberMe
- IsLoading
- ErrorMessage

**Commands**:
- LoginCommand
- NavigateToRegisterCommand

#### 4. RegisterViewModel
**Purpose**: Registration logic

**Properties**:
- Username
- Email
- Password
- ConfirmPassword
- DisplayName
- IsLoading
- ErrorMessage

**Commands**:
- RegisterCommand
- NavigateToLoginCommand

#### 5. ChatViewModel
**Purpose**: Chat list and management

**Properties**:
- Chats (ObservableCollection)
- SelectedChat
- SearchQuery
- Folders

**Commands**:
- SelectChatCommand
- CreateChatCommand
- CreateFolderCommand
- SearchCommand
- RefreshCommand
- StartCallCommand

#### 6. ConversationViewModel
**Purpose**: Message display and sending

**Properties**:
- Messages (ObservableCollection)
- CurrentChat
- MessageText
- IsTyping
- Attachments

**Commands**:
- SendMessageCommand
- EditMessageCommand
- DeleteMessageCommand
- ReplyToMessageCommand
- AttachFileCommand
- TranslateMessageCommand

#### 7. MessageViewModel
**Purpose**: Individual message representation

**Properties**:
- MessageId
- Content
- Sender
- Timestamp
- Status
- IsEdited
- ReplyTo
- Attachments

#### 8. SettingsViewModel
**Purpose**: Settings management

**Properties**:
- SelectedTheme
- SelectedLanguage
- NotificationSettings
- PrivacySettings

**Commands**:
- SaveSettingsCommand
- ResetSettingsCommand

### Supporting ViewModels

- **CallViewModel**: Voice call interface and management
  - Call state (idle, ringing, connecting, connected)
  - Call duration tracking
  - Call controls (answer, reject, end, mute)
  - Audio level indicators
- AttachmentViewModel
- ChatFolderViewModel
- CreateChatDialogViewModel
- CreateFolderDialogViewModel
- DateSeparatorViewModel
- FilesListViewModel
- GeneratorViewModel
- ImagesGridViewModel
- ImageViewModel
- SelectableUserViewModel
- TranslateWindowViewModel

---

## Services

### Core Services

#### 1. AuthenticationService
**Purpose**: Authentication and session management

**Methods**:
- `LoginAsync(usernameOrEmail, password, rememberMe)`
- `RegisterAsync(registerRequest)`
- `LogoutAsync()`
- `TryRestoreSessionAsync()`
- `RefreshTokenAsync()`

**Features**:
- JWT token management
- Secure token storage (LiteDB)
- Session restoration
- Token refresh

#### 2. MessagingService
**Purpose**: WebSocket client for real-time messaging

**Methods**:
- `ConnectAsync(serverUrl, token)`
- `DisconnectAsync()`
- `SendMessageAsync(chatId, content, replyToId)`
- `EditMessageAsync(messageId, content)`
- `DeleteMessageAsync(messageId)`

**Features**:
- WebSocket connection management
- Automatic reconnection with exponential backoff
- Message sending and receiving
- Real-time updates
- Heartbeat mechanism
- Connection state management
- Call message routing (CallMessageReceived event)

#### 3. CallService
**Purpose**: Voice call management and WebRTC signaling

**Methods**:
- `StartCallAsync(userId, chatId)`
- `AnswerCallAsync(callId)`
- `RejectCallAsync(callId)`
- `EndCallAsync(callId)`
- `SendSdpOfferAsync(callId, sdp)`
- `SendSdpAnswerAsync(callId, sdp)`
- `SendIceCandidateAsync(callId, candidate)`
- `SendAudioDataAsync(callId, audioData)`

**Features**:
- Voice call initiation and management
- WebRTC signaling via WebSocket
- Audio capture using NAudio (WaveInEvent)
- Audio playback using NAudio (WaveOutEvent)
- Call state management (idle, initiating, ringing, connecting, connected, ended)
- ICE candidate exchange for NAT traversal
- SDP offer/answer exchange for WebRTC
- Audio data streaming
- Call event handling (IncomingCall, CallStateChanged, CallEnded)
- Automatic cleanup on call end

#### 4. NavigationService
**Purpose**: View navigation

**Methods**:
- `NavigateTo<TViewModel>()`
- `NavigateBack()`
- `CanNavigateBack`

**Features**:
- Type-based navigation
- Navigation history
- View model lifecycle management

#### 5. UserDirectoryService
**Purpose**: User directory management

**Methods**:
- `SearchUsersAsync(query)`
- `GetUserAsync(userId)`
- `UpdateUserAsync(userId, updates)`

#### 6. FileAttachmentService
**Purpose**: File handling

**Methods**:
- `UploadFileAsync(filePath, chatId)`
- `DownloadFileAsync(attachmentId, savePath)`
- `GetAttachmentsAsync(chatId)`

**Features**:
- File upload
- File download
- File type validation
- File size limits

#### 7. AvatarService
**Purpose**: Avatar management

**Methods**:
- `GetAvatarUrl(userId)`
- `UploadAvatarAsync(imagePath)`
- `GetDefaultAvatar(username)`

#### 8. ImageGeneratorService
**Purpose**: AI image generation

**Methods**:
- `GenerateImageAsync(prompt, parameters)`
- `GetGeneratedImagesAsync()`
- `DeleteImageAsync(imageId)`

#### 9. ImageCompressionService
**Purpose**: Image optimization

**Methods**:
- `CompressImageAsync(imagePath, maxSize)`
- `ResizeImageAsync(imagePath, width, height)`

#### 10. TranslationService
**Purpose**: Message translation

**Methods**:
- `TranslateAsync(text, sourceLanguage, targetLanguage)`
- `DetectLanguageAsync(text)`

#### 11. ErrorHandlingService
**Purpose**: Error management

**Methods**:
- `HandleErrorAsync(exception, severity)`
- `ShowErrorAsync(message, severity)`

**Features**:
- Error logging
- User-friendly error messages
- Error notification events

#### 12. CredentialStorageService
**Purpose**: Secure credential storage

**Methods**:
- `SaveCredentialsAsync(username, password)`
- `GetCredentialsAsync()`
- `ClearCredentialsAsync()`

**Features**:
- Encrypted storage (LiteDB)
- Secure credential management

#### 13. OfflineMessageQueue
**Purpose**: Offline message queuing

**Methods**:
- `EnqueueMessageAsync(message)`
- `ProcessQueueAsync()`
- `ClearQueueAsync()`

**Features**:
- Message queuing when offline
- Automatic sending when online
- Queue persistence

---

## Infrastructure

### MessageBus

**Purpose**: Loosely-coupled component communication

**Usage**:
```csharp
// Subscribe
messageBus.Subscribe<ThemeChangedMessage>(msg => {
    // Handle theme change
});

// Publish
messageBus.Publish(new ThemeChangedMessage(AppTheme.Dark));
```

**Message Types**:
- NavigationRequestMessage
- ThemeChangedMessage
- UserStatusChangedMessage
- (Custom messages as needed)

### RelayCommand

**Purpose**: Command pattern implementation

**Usage**:
```csharp
[RelayCommand]
private async Task DoSomething()
{
    // Command implementation
}
```

Automatically generates `DoSomethingCommand` property.

### DependencyInjection

**Service Registration**:
- Services registered in `DependencyInjection.cs`
- ViewModels registered as transient
- Views registered as transient
- Services registered with appropriate lifetimes

---

## Themes and Styling

### Theme Support

**Themes Available**:
- Light theme
- Dark theme (default)

**Theme Resources**:
- `Themes/DarkTheme.xaml` - Dark theme resources
- `Themes/Styles.xaml` - Common styles
- `Themes/Converters.xaml` - Value converters

### Value Converters

**Converters Available** (16 converters):
- BooleanToVisibilityConverter
- InverseBooleanToVisibilityConverter
- BooleanToAlignmentConverter
- BooleanToBackgroundConverter
- BooleanToOpacityConverter
- BooleanToStringConverter
- CountToVisibilityConverter
- FirstCharacterConverter
- NullToBooleanConverter
- NullToVisibilityConverter
- PromptPreviewConverter
- StringToVisibilityConverter
- UserStatusToBrushConverter
- UserStatusToTextConverter
- InverseBooleanConverter

### Styling

- Consistent color scheme
- Modern UI elements
- Smooth animations
- Responsive layout
- Custom window chrome

---

## Communication

### REST API Client

**HTTP Client Configuration**:
- Base URL from server configuration
- JWT token in Authorization header
- Automatic token refresh
- Error handling

**Endpoints Used**:
- `/api/auth/*` - Authentication
- `/api/users/*` - User management
- `/api/chats/*` - Chat operations
- `/api/messages/*` - Message operations
- `/api/attachments/*` - File operations
- `/api/preferences/*` - Preferences
- `/api/chat-folders/*` - Folder operations
- `/api/generated-images/*` - Image generation

### WebSocket Client

**Connection**:
- Endpoint: `ws://{server}:{port}/ws`
- Authentication via JWT token
- Automatic reconnection
- Heartbeat mechanism

**Message Handling**:
- Real-time message delivery
- Status updates
- Presence updates
- Typing indicators

**Reconnection Strategy**:
- Exponential backoff
- Maximum retry attempts
- Connection state management

---

## Local Storage

### LiteDB

**Purpose**: Client-side data storage

**Stored Data**:
- User credentials (encrypted)
- Cached user data
- Offline messages
- Application settings
- Session tokens

**Location**:
- `%LocalAppData%\NexusTeam\Data\`

---

## Error Handling

### Error Handling Strategy

1. **Service-Level Errors**: Caught and logged
2. **User-Friendly Messages**: Generic error messages shown to users
3. **Error Notifications**: Error notification events
4. **Retry Logic**: Automatic retry for transient errors
5. **Offline Handling**: Queue messages when offline

### Error Severity Levels

- **Info**: Informational messages
- **Warning**: Warning messages
- **Error**: Error messages
- **Critical**: Critical errors requiring attention

---

## Logging

### Serilog Configuration

**Log Location**: `%LocalAppData%\NexusTeam\Logs\`

**Log File**: `NexusTeam-client-YYYY-MM-DD.log`

**Log Levels**:
- Debug: Detailed execution flow
- Information: General information
- Warning: Warnings
- Error: Errors
- Fatal: Critical errors

**Log Retention**: 7 days

### Logged Events

- Application startup/shutdown
- Authentication events
- WebSocket connections
- API requests/responses
- Errors and exceptions
- User actions

---

## Build and Run

### Prerequisites

Before running the client, ensure:
1. **Docker services are running** (see [INSTALLATION.md](INSTALLATION.md))
2. **Server is running** (see [SERVER.md](SERVER.md))

### Building the Client

```powershell
# Navigate to client directory
cd src\NexusTeam.Client

# Restore dependencies
dotnet restore

# Build
dotnet build

# Build in Release mode
dotnet build -c Release
```

### Running the Client

```powershell
# Run with dotnet
dotnet run localhost 5251

# Or run compiled executable
.\bin\Debug\net8.0-windows\NexusTeam.exe localhost 5251
```

> [!NOTE]
> The server must be running and connected to Docker services (Oracle, MongoDB, Redis) before the client can connect.

### Command-Line Arguments

**Required Arguments**:
1. Server IP address or hostname (e.g., `localhost`, `127.0.0.1`)
2. Server port number (e.g., `5251`, `5000`)

**Examples**:
```powershell
NexusTeam.exe localhost 5251
NexusTeam.exe 127.0.0.1 5251
NexusTeam.exe 192.168.1.100 8080
```

### Application Startup

1. Parse command-line arguments
2. Configure logging
3. Configure exception handling
4. Initialize dependency injection
5. Build host
6. Start host
7. Try to restore session
8. Navigate to appropriate view
9. Show main window

---

## Configuration

### appsettings.json

Minimal configuration file, typically doesn't require changes.

### Server Configuration

Server configuration passed via command-line arguments:
- Server IP address
- Server port

### User Preferences

User preferences stored in MongoDB and synced:
- Theme preference
- Language preference
- Notification settings
- Privacy settings

---

## Features in Detail

### Real-Time Messaging

- **WebSocket Connection**: Persistent connection for real-time updates
- **Automatic Reconnection**: Reconnects on connection loss
- **Message Delivery**: Real-time message delivery
- **Status Updates**: Real-time status updates (sent, delivered, read)
- **Typing Indicators**: Real-time typing indicators

### File Attachments

- **File Upload**: Drag-and-drop or file picker
- **File Types**: Support for various file types
- **File Size Limits**: Configurable file size limits
- **File Preview**: Preview for images and documents
- **File Download**: Download attached files

### Code Preview

- **Syntax Highlighting**: AvalonEdit integration
- **Language Detection**: Automatic language detection
- **Multiple Languages**: Support for many programming languages
- **Copy to Clipboard**: Easy code copying

### Translation

- **Message Translation**: Translate messages to different languages
- **Language Detection**: Automatic source language detection
- **Multiple Languages**: Support for many languages

### Image Generation

- **AI Integration**: AI-powered image generation
- **Prompt Input**: Text prompt for image generation
- **Parameter Control**: Generation parameters
- **Image Gallery**: View generated images

### Offline Support

- **Message Queue**: Queue messages when offline
- **Automatic Sending**: Send queued messages when online
- **Queue Persistence**: Queue persisted to disk

---

## Performance Considerations

### UI Responsiveness

- **Async Operations**: All I/O operations are async
- **Virtualization**: Message list virtualization
- **Lazy Loading**: Load data on demand
- **Caching**: Cache frequently accessed data

### Memory Management

- **Disposal**: Proper disposal of resources
- **Weak References**: Use weak references where appropriate
- **Collection Management**: Efficient collection management

### Network Optimization

- **Connection Reuse**: Reuse HTTP connections
- **Compression**: Message compression (future)
- **Batch Operations**: Batch API calls where possible

---

## Troubleshooting

### Common Issues

**Issue**: Cannot connect to server
- Verify Docker services are running: `docker-compose ps`
- Verify server is running and connected to Docker services
- Check server IP and port
- Check firewall settings
- Verify network connectivity

**Issue**: Messages not sending
- Check WebSocket connection
- Verify authentication token
- Check server logs
- Verify network connectivity

**Issue**: Application crashes
- Check logs in `%LocalAppData%\NexusTeam\Logs\`
- Verify .NET 8.0 runtime installed
- Check system requirements

**Issue**: Session not restoring
- Clear credentials and re-login
- Check LiteDB database
- Verify token validity

---

This documentation provides a comprehensive overview of the Nexus Team Client implementation. For specific implementation details, refer to the source code and inline documentation.

