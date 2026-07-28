# NexusTeam MVP - Architecture Overview

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Component Architecture](#component-architecture)
3. [Data Architecture](#data-architecture)
4. [Communication Protocols](#communication-protocols)
5. [Security Architecture](#security-architecture)
6. [Scalability Considerations](#scalability-considerations)
7. [Monitoring and Observability](#monitoring-and-observability)
8. [Deployment Architecture](#deployment-architecture)
9. [Technology Stack](#technology-stack)
10. [Future Enhancements](#future-enhancements)

---

## System Architecture

NexusTeam is a real-time chat application built with a modern, distributed architecture using .NET 8.

### High-Level Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                          Client Layer                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│   ┌─────────────────────────────┐  ┌──────────────────────────┐ │
│   │  Web SPA (Nexus-Team/web)   │  │  WPF Client (optional)   │ │
│   │  - Responsive HTML/CSS/JS   │  │  - Windows-only (.NET 8) │ │
│   │  - Desktop + mobile browser │  │  - MVVM + NAudio calls   │ │
│   │  - Nginx serves SPA         │  │  - Offline message queue │ │
│   │  - Proxies /api and /ws     │  │                          │ │
│   │  Port 8080 (universal UI)   │  │                          │ │
│   └──────────────┬──────────────┘  └────────────┬─────────────┘ │
│                  │  WebSocket + HTTP/REST        │               │
│                  └───────────────┬───────────────┘               │
└──────────────────────────────────┼────────────────────────────────┘
                                   │
┌──────────────────────────────────┼────────────────────────────────┐
│                         Server Layer                              │
├──────────────────────────────────┼────────────────────────────────┤
│                                  ▼                                 │
│   ┌───────────────────────────────────────────────────────┐      │
│   │      ASP.NET Core Web API (NexusTeam.Server)          │      │
│   │  - RESTful API Controllers                             │      │
│   │  - WebSocket Connection Manager                        │      │
│   │  - JWT Authentication                                  │      │
│   │  - Real-time Message Broadcasting                      │      │
│   │  - Voice Call Signaling                                │      │
│   │  - Rate Limiting                                       │      │
│   │  - Security Headers                                    │      │
│   │  - Input Validation                                    │      │
│   └───────────────────────────────────────────────────────┘      │
│                              │                                     │
│              ┌───────────────┼───────────────┐                   │
│              │               │               │                    │
│              ▼               ▼               ▼                    │
└──────────────────────────────────────────────────────────────────┘
               │               │               │
┌──────────────┼───────────────┼───────────────┼──────────────────┐
│         Data Layer           │               │                   │
├──────────────┼───────────────┼───────────────┼──────────────────┤
│              ▼               ▼               ▼                   │
│  ┌────────────────┐  ┌──────────────┐  ┌──────────────┐        │
│  │  Oracle DB     │  │  MongoDB     │  │    Redis     │        │
│  │                │  │              │  │              │        │
│  │ - Users        │  │ - Messages   │  │ - Sessions   │        │
│  │                │  │ - Chats      │  │ - Presence   │        │
│  │                │  │ - Attachments│  │ - Cache      │        │
│  │                │  │ - Preferences│  │ - Queues     │        │
│  │                │  │ - Folders    │  │ - Rate Limits│        │
│  └────────────────┘  └──────────────┘  └──────────────┘        │
└──────────────────────────────────────────────────────────────────┘
```

Cross-platform access is provided by the **responsive web client**: open `http://localhost:8080` (or `http://<host-ip>:8080` from another device on the same network) in any modern browser on desktop or mobile. The WPF app remains available for Windows development and voice-call features.

---

## Component Architecture

### 1. NexusTeam.Shared (Shared Library)

**Purpose**: Common code, models, and utilities shared between client and server.

**Key Components**:
- **Domain Models**: Core entities (`User`, `Chat`, `Message`, `MessageAttachment`, `ChatFolder`)
- **DTOs**: Data transfer objects for API communication (`UserDto`, `ChatDto`, `MessageDto`, etc.)
- **Contracts**: WebSocket message contracts (typed messages for real-time communication and voice calls)
- **Enums**: Status enumerations (`UserStatus`, `ChatType`, `MessageStatus`)
- **Abstractions**: Interfaces for dependency injection (`IClock`, `IIdGenerator`, `IPasswordHasher`)
- **Helpers**: Utility classes (password hashing, Redis key generation, pagination)
- **Configuration**: Settings classes (`JwtSettings`, `PasswordHashingOptions`)
- **Serialization**: Source-generated JSON serializer context for high performance
- **Exceptions**: Custom exception types (`UserNotFoundException`, `ChatNotFoundException`, etc.)

**Design Patterns**:
- Repository pattern interfaces
- Dependency injection abstractions
- Value objects for configuration
- Factory pattern for serialization

### 2. NexusTeam.Server (ASP.NET Core Web API)

**Purpose**: Backend server providing REST API and WebSocket support.

**Key Components**:

#### API Controllers (8 Controllers)

1. **AuthController**: User registration, login, logout, token refresh
2. **UsersController**: User profile management, avatar upload, user search
3. **ChatsController**: Chat creation, management, participant management
4. **MessagesController**: Message operations (send, edit, delete, history)
5. **AttachmentsController**: File upload, download, management
6. **PreferencesController**: User preferences management
7. **ChatFoldersController**: Chat folder organization
8. **GeneratedImagesController**: AI-generated image management

#### Services

**Core Services**:
- **AuthService**: User authentication, registration, password management
- **JwtTokenService**: JWT token generation and validation
- **RefreshTokenService**: Refresh token management
- **UserService**: User CRUD operations
- **ChatService**: Chat management and participants
- **MessageService**: Message handling and history
- **AttachmentService**: File attachment handling
- **AvatarService**: Avatar image processing
- **GeneratedImageService**: AI image generation management

**Infrastructure Services**:
- **WebSocketConnectionManager**: Real-time connection management
- **UserStatusService**: User presence tracking
- **SessionService**: Session management
- **RateLimitService**: API rate limiting
- **RedisCacheService**: Caching operations
- **PresenceTrackingService**: User presence tracking

#### Repositories

**Oracle Repositories**:
- **OracleUserRepository**: User data persistence (only repository for Oracle)

**MongoDB Repositories**:
- **MongoChatRepository**: Chat data persistence
- **MongoMessageRepository**: Message data persistence
- **MongoMessageAttachmentRepository**: Attachment metadata
- **MongoUserPreferenceRepository**: User preferences
- **MongoChatFolderRepository**: Chat folder organization
- **MongoGeneratedImageRepository**: Generated images storage

**Redis Services**:
- **RedisCacheService**: Caching and session management
- **RateLimitService**: Rate limiting counters

#### Middleware

1. **JwtAuthenticationMiddleware**: Token validation and user context
2. **WebSocketHandler**: WebSocket connection handling (messaging and voice call signaling)
3. **ExceptionHandlingMiddleware**: Global error handling
4. **SecurityHeadersMiddleware**: Security headers injection
5. **RequestLoggingMiddleware**: Request/response logging

**Design Patterns**:
- MVC/API pattern
- Repository pattern
- Dependency injection
- Middleware pipeline
- Observer pattern (WebSocket broadcasting)
- Strategy pattern (reconnection strategies)

### 3. Web Client (`Nexus-Team/web`) — Primary Universal UI

**Purpose**: Cross-platform responsive SPA served by Nginx. This is the default client for all devices.

**Stack**: Static HTML / CSS / JavaScript, Nginx reverse proxy for `/api` and `/ws`.

**Key capabilities**:
- Auth (login / register), real-time messaging over WebSocket
- Chats, groups, folders, attachments, emoji, voice messages (`MediaRecorder`)
- AI image generation, presence, preferences
- Responsive layout: side-by-side on desktop; full-screen chat navigation on phones (`≤768px`)

Open **http://localhost:8080** after `docker compose up`. No native app install is required.

### 4. NexusTeam.Client (WPF Desktop Application) — Optional, Windows-only

**Purpose**: Optional Windows desktop client (MVVM). Useful for development and for WebRTC voice calls via NAudio. Not required to use the product.

**Architecture**: MVVM (Model-View-ViewModel)

**Key Components**:

#### Views (16 Views)

1. **MainWindow**: Main application window with navigation
2. **WelcomeView**: Welcome/login screen
3. **LoginView**: User login interface
4. **RegisterView**: User registration interface
5. **ChatView**: Main chat interface
6. **ConversationView**: Message conversation display
7. **SettingsView**: Application settings
8. **GeneratorView**: AI image generation interface
9. **FilesListView**: File management view
10. **ImagesGridView**: Generated images gallery
11. **CodePreviewWindow**: Code snippet preview
12. **TranslateWindow**: Translation interface
13. **CreateChatDialog**: Chat creation dialog
14. **CreateFolderDialog**: Folder creation dialog
15. Additional utility views

#### ViewModels (21 ViewModels)

1. **MainWindowViewModel**: Application state and navigation
2. **WelcomeViewModel**: Welcome screen logic
3. **LoginViewModel**: Login/authentication logic
4. **RegisterViewModel**: Registration logic
5. **ChatViewModel**: Chat list and management
6. **ConversationViewModel**: Message display and sending
7. **MessageViewModel**: Individual message representation
8. **SettingsViewModel**: Settings management
9. **GeneratorViewModel**: Image generation logic
10. **FilesListViewModel**: File management
11. **ImagesGridViewModel**: Image gallery
12. **AttachmentViewModel**: File attachment handling
13. **ChatFolderViewModel**: Folder management
14. **CreateChatDialogViewModel**: Chat creation
15. **CreateFolderDialogViewModel**: Folder creation
16. **TranslateWindowViewModel**: Translation logic
17. **CallViewModel**: Voice call interface and management
18. Additional supporting view models

#### Services

**Core Services**:
- **AuthenticationService**: Token storage and management
- **MessagingService**: WebSocket client for real-time messaging
- **CallService**: Voice call management and WebRTC signaling (NAudio)
- **NavigationService**: View navigation
- **UserDirectoryService**: User directory management
- **FileAttachmentService**: File handling
- **AvatarService**: Avatar management
- **ImageGeneratorService**: AI image generation
- **ImageCompressionService**: Image optimization
- **TranslationService**: Message translation
- **ErrorHandlingService**: Error management
- **CredentialStorageService**: Secure credential storage
- **OfflineMessageQueue**: Offline message queuing

**Infrastructure**:
- **MessageBus**: Loosely-coupled component communication
- **RelayCommand**: Command pattern implementation

**Design Patterns**:
- MVVM pattern
- Command pattern (RelayCommand)
- Observable collections
- Dependency injection
- Service locator
- Observer pattern (MessageBus)

---

## Data Architecture

### Database Schema

#### Oracle Database (User Data Store)

**Purpose**: Stores user account information and authentication data.

**Tables**:

1. **users**
   - Primary key: `id` (VARCHAR2)
   - Unique constraints: `username`, `email`
   - Indexes: username, email, status, created_at
   - Audit: created_at, updated_at, last_seen_at
   - Fields: id, username, email, password_hash, display_name, avatar_url, status, created_at, updated_at, last_seen_at
   - Constraints: status check (0=Offline, 1=Online, 2=Away, 3=DND)

**Why Oracle for Users**:
- Relational integrity for user accounts
- Strong ACID guarantees for authentication data
- Efficient joins and queries for user lookups
- Traditional SQL-based user management

#### MongoDB (Message and Chat Data Store)

**Purpose**: Stores messages, chats, attachments, and user preferences with flexible schema.

**Collections**:

1. **messages**
   - Document structure with schema validation
   - Required fields: chatId, senderId, content, status, createdAt, isDeleted
   - Optional fields: editedAt, replyToId, reactions
   - Indexes: chatId, senderId, createdAt, replyToId, isDeleted, composite (chatId, createdAt)
   - Status enum: 0=Sent, 1=Delivered, 2=Read, 3=Failed
   - Content max length: 4000 characters
   - Reactions: Dictionary of emoji to list of user IDs

2. **chats**
   - Document structure with schema validation
   - Required fields: name, type, participants, createdBy, createdAt, updatedAt
   - Optional fields: description, avatarUrl, lastMessageAt
   - Type enum: "private", "group", "channel"
   - Participants: Array of user IDs
   - Indexes: type, createdBy, createdAt, lastMessageAt, participants

3. **attachments**
   - Document structure with schema validation
   - Required fields: messageId, fileName, filePath, fileSize, contentType, attachmentType, uploadedAt
   - Optional fields: thumbnailPath
   - Attachment types: 0=Image, 1=Video, 2=Audio, 3=Document, 4=Archive, 5=Code, 99=Other
   - File size limit: 100MB (104857600 bytes)
   - Indexes: messageId, uploadedAt, attachmentType

4. **user_preferences**
   - Document per user (userId as unique key)
   - Schema validation enabled
   - Flexible nested structure for preferences
   - TTL index: auto-delete after 180 days of inactivity
   - Fields:
     - userId (required, unique)
     - theme: "light", "dark", or "auto"
     - language: ISO 639-1 format (e.g., "en", "en-US")
     - notificationSettings: object with enableSound, enableDesktop, enableEmail, muteUntil
     - privacySettings: object with showOnlineStatus, showLastSeen, allowDirectMessages, readReceipts
     - chatSettings: object with fontSize, enterToSend, showTimestamps, compactMode
     - customSettings: flexible key-value pairs
     - createdAt, updatedAt (required)

5. **chat_folders** (stored in MongoDB)
   - User-specific folder organization
   - Flexible structure for folder hierarchy
   - Indexes: userId

**Why MongoDB for Messages/Chats**:
- Flexible schema for evolving message features (reactions, threading)
- Document-oriented structure matches message data model
- Horizontal scaling for high message volume
- Rich indexing for fast queries
- Schema validation ensures data integrity

**Advantages**:
- Schema flexibility for evolving features
- Fast read/write for message and chat operations
- JSON-like structure matches client models
- Efficient indexing for queries
- TTL indexes for automatic cleanup

#### Redis (In-Memory Data Store)

**Key Structures**:

1. **Sessions**: Hash (userId, username, timestamps)
   - TTL: 24 hours
   - Pattern: `NexusTeam:session:{sessionId}`

2. **User Presence**: Hash (status, lastSeen, connectionCount)
   - TTL: 5 minutes (refreshed on activity)
   - Pattern: `NexusTeam:presence:{userId}`

3. **WebSocket Connections**: Hash (connection metadata)
   - TTL: 1 hour (refreshed with heartbeat)
   - Pattern: `NexusTeam:ws:{userId}:{connectionId}`

4. **Rate Limiting**: Counter
   - TTL: 1-5 minutes (sliding window)
   - Pattern: `NexusTeam:ratelimit:{userId}:{endpoint}`

5. **Cache**: Hash (serialized data)
   - TTL: 10-15 minutes
   - Pattern: `NexusTeam:cache:{type}:{id}`

6. **Message Queue**: List (offline messages)
   - TTL: 1 hour
   - Pattern: `NexusTeam:queue:messages:{chatId}`

7. **Refresh Tokens**: String (token value)
   - TTL: 7 days
   - Pattern: `NexusTeam:refresh:{userId}:{tokenId}`

**Advantages**:
- Sub-millisecond latency
- Automatic TTL expiration
- Atomic operations for counters
- Pub/Sub for real-time features

---

## Communication Protocols

### REST API (HTTP)

**Base URL**: `http://localhost:{port}/api`

**Endpoints**:

**Authentication**:
```
POST   /auth/register          - Register new user
POST   /auth/login             - Login and get JWT token
POST   /auth/logout             - Logout and invalidate token
POST   /auth/refresh           - Refresh access token
```

**Users**:
```
GET    /users/{id}             - Get user by ID
PUT    /users/{id}             - Update user profile
GET    /users                  - Search users
POST   /users/{id}/avatar      - Upload avatar
```

**Chats**:
```
GET    /chats                  - Get user's chats
POST   /chats                  - Create new chat
GET    /chats/{id}             - Get chat details
PUT    /chats/{id}             - Update chat
DELETE /chats/{id}             - Delete chat
```

**Messages**:
```
POST   /chats/{id}/messages    - Send message
GET    /chats/{id}/messages    - Get message history
PUT    /messages/{id}          - Edit message
DELETE /messages/{id}          - Delete message
```

**Attachments**:
```
POST   /attachments             - Upload file
GET    /attachments/{id}       - Download file
DELETE /attachments/{id}       - Delete file
```

**Preferences**:
```
GET    /preferences            - Get user preferences
PUT    /preferences            - Update preferences
```

**Chat Folders**:
```
GET    /chat-folders           - Get folders
POST   /chat-folders          - Create folder
PUT    /chat-folders/{id}      - Update folder
DELETE /chat-folders/{id}      - Delete folder
```

**Authentication**: Bearer JWT token in Authorization header

**Request/Response Format**: JSON

### WebSocket (Real-Time)

**Endpoint**: `ws://localhost:{port}/ws`

**Authentication**: JWT token passed as query parameter or in first message

**Message Format**: JSON with type discriminator

**Contract Types**:
- `UserJoinedContract` - User joined chat
- `UserLeftContract` - User left chat
- `UserStatusChangedContract` - User status update
- `MessageSentContract` - New message
- `MessageEditedContract` - Message edited
- `MessageDeletedContract` - Message deleted
- `TypingIndicatorContract` - User typing indicator
- `MessageDeliveredContract` - Message delivery receipt
- `MessageReadContract` - Message read receipt
- `ErrorContract` - Error notification

**Flow**:
1. Client establishes WebSocket connection
2. Client sends authentication message with JWT token
3. Server validates token and associates connection with user
4. Server subscribes client to relevant chat channels
5. Bidirectional message exchange
6. Heartbeat to keep connection alive
7. Automatic reconnection on disconnect

---

## Security Architecture

### Authentication

**Method**: JWT (JSON Web Tokens)

**Token Structure**:
```json
{
  "sub": "user_id",
  "username": "john.doe",
  "email": "john@example.com",
  "jti": "token_unique_id",
  "iat": 1234567890,
  "exp": 1234571490
}
```

**Token Lifetime**: 60 minutes (configurable)

**Refresh Strategy**: 
- Refresh tokens stored in Redis with 7-day TTL
- Client must refresh before expiration
- Refresh tokens can be revoked

**Token Storage**:
- Client: Secure storage (encrypted with LiteDB)
- Server: Blacklist in Redis for revoked tokens

### Password Security

**Hashing**: BCrypt with work factor 11 (configurable)

**Validation Rules**:
- Minimum 8 characters
- Must contain uppercase, lowercase, digit, special character
- Not in common password list

**Storage**: Only hashed password stored in database (password_hash column)

### Authorization

**Method**: Role-based access control (future enhancement)

**Current Implementation**: 
- User can only access their own data
- User must be chat participant to access chat/messages
- Message author or chat owner can delete messages
- Chat creator can manage participants

### API Security

**Rate Limiting**:
- Login: 5 attempts per 5 minutes per identifier
- Messages: 60 messages per minute per user
- General API: Configurable per endpoint

**CORS**: Configurable allowed origins

**SSL/TLS**: Required in production (disabled in dev)

**Input Validation**:
- FluentValidation for all endpoints
- Model validation with data annotations
- XSS protection via encoding
- SQL injection protection via parameterized queries

**Security Headers**:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Content-Security-Policy: Configured per environment

### Data Security

**Encryption**:
- At rest: Database encryption (Oracle TDE, MongoDB encryption)
- In transit: TLS/SSL for all connections
- Sensitive fields: Additional encryption for passwords (BCrypt)

**Audit Logging**:
- Message editing tracked via editedAt field in MongoDB
- Message deletion tracked via isDeleted flag in MongoDB
- User actions logged via Serilog
- Security events logged
- Call events logged for voice calls

**Privacy**:
- User preferences control visibility settings
- Soft delete for messages (isDeleted flag)
- GDPR compliance considerations (right to be forgotten)

---

## Scalability Considerations

### Horizontal Scaling

**Stateless API Servers**:
- Multiple server instances behind load balancer
- JWT tokens allow any server to validate requests
- No server-side session state

**WebSocket Scaling**:
- Sticky sessions for WebSocket connections
- Redis Pub/Sub for cross-server message broadcasting
- Connection count tracking in Redis

**Database Scaling**:
- Oracle: Read replicas for read-heavy operations
- MongoDB: Sharding for user preferences
- Redis: Redis Cluster for high availability

### Vertical Scaling

**Database Optimization**:
- Proper indexing on frequently queried columns
- Composite indexes for common query patterns
- Partitioning for large tables (messages)

**Caching Strategy**:
- Redis cache for frequently accessed data
- Cache-aside pattern with TTL
- Cache invalidation on updates

**Connection Pooling**:
- Database connection pools
- Redis connection multiplexing
- HTTP client connection reuse

### Performance Optimization

**Message Pagination**: 
- Cursor-based pagination for message history
- Limit: 50 messages per page

**Lazy Loading**:
- User profiles loaded on demand
- Chat details fetched only when opened

**Batch Operations**:
- Bulk message delivery via WebSocket
- Batch status updates (read receipts)

**Compression**:
- WebSocket message compression
- HTTP response compression (Gzip)

---

## Monitoring and Observability

### Logging

**Framework**: Serilog

**Log Levels**:
- Error: Exceptions and critical issues
- Warning: Validation failures, rate limiting
- Information: API requests, WebSocket events
- Debug: Detailed execution flow (dev only)

**Log Sinks**:
- Console (development)
- File (production)
- Future: Application Insights, ELK stack

### Metrics

**Application Metrics**:
- Request count and latency
- WebSocket connection count
- Message throughput
- Cache hit/miss ratio

**Database Metrics**:
- Query execution time
- Connection pool utilization
- Table sizes and growth

**System Metrics**:
- CPU and memory usage
- Network I/O
- Disk I/O

### Health Checks

**Endpoints**:
- `/health` - Overall health status
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

**Checks**:
- Database connectivity (Oracle, MongoDB, Redis)
- Disk space availability
- Memory usage

---

## Deployment Architecture

### Development Environment (Docker)

```
Developer Machine
├── Docker Desktop / Docker Engine
│   ├── Oracle XE 21c (Container)
│   ├── MongoDB Sharded Cluster (Containers)
│   │   ├── Config Server
│   │   ├── Shard 1
│   │   ├── Shard 2
│   │   └── Mongos Router
│   ├── Redis (Container)
│   └── Database Seeder (Container)
├── NexusTeam.Server (.NET 8.0 - Host)
└── NexusTeam.Client (.NET 8.0 - Host)
```

**Docker Services:**
- All databases run in Docker containers
- Application runs on host machine
- Pre-configured connection strings in `appsettings.json`
- Automatic database initialization via seeder

**Connection Details:**
- Oracle: `localhost:1530/XEPDB1`
- MongoDB: `mongodb://localhost:27018`
- Redis: `localhost:6380`

### Production Environment (Future)

```
┌─────────────────────────────────────────────┐
│              Load Balancer                  │
├─────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐    │
│  │ Server1 │  │ Server2 │  │ Server3 │    │
│  └─────────┘  └─────────┘  └─────────┘    │
└─────────────────────────────────────────────┘
              │         │         │
       ┌──────┘         │         └──────┐
       ▼                ▼                 ▼
┌─────────────┐  ┌─────────────┐  ┌─────────────┐
│   Oracle    │  │  MongoDB     │  │   Redis     │
│   Cluster   │  │  Sharded     │  │   Cluster   │
│   (Docker)  │  │  Cluster     │  │   (Docker)  │
│             │  │  (Docker)     │  │             │
└─────────────┘  └─────────────┘  └─────────────┘
```

### Docker Infrastructure

**Container Architecture:**

```
┌─────────────────────────────────────────────────┐
│              Docker Network                      │
│              (nexusteam_network)                     │
├─────────────────────────────────────────────────┤
│                                                  │
│  ┌──────────────┐  ┌──────────────┐           │
│  │   Oracle XE  │  │    Redis      │           │
│  │   Port: 1530  │  │  Port: 6380   │           │
│  └──────────────┘  └──────────────┘           │
│                                                  │
│  ┌──────────────────────────────────────────┐   │
│  │      MongoDB Sharded Cluster             │   │
│  │  ┌────────────┐  ┌────────────┐         │   │
│  │  │ Config Svr │  │  Shard 1  │         │   │
│  │  │ Port: 27019│  │ Port:27020│         │   │
│  │  └────────────┘  └────────────┘         │   │
│  │  ┌────────────┐  ┌────────────┐         │   │
│  │  │  Shard 2   │  │   Mongos   │         │   │
│  │  │ Port:27021│  │ Port:27018 │         │   │
│  │  └────────────┘  └────────────┘         │   │
│  └──────────────────────────────────────────┘   │
│                                                  │
│  ┌──────────────┐                               │
│  │ DB Seeder    │                               │
│  │ (One-time)  │                               │
│  └──────────────┘                               │
└─────────────────────────────────────────────────┘
```

**Data Persistence:**
- Docker volumes for all database data
- Persistent storage across container restarts
- Volume names: `nexusteam_oracle_data`, `nexusteam_redis_data`, `nexusteam_mongo_*_data`

**Network Configuration:**
- Bridge network: `nexusteam_network`
- Internal DNS resolution between containers
- Port mapping for host access

---

## Technology Stack Summary

| Layer | Technology | Purpose |
|-------|------------|---------|
| Primary client | Responsive web SPA + Nginx | Universal UI (desktop + mobile browsers) |
| Optional client | WPF (.NET 8 Windows) | Desktop UI, voice calls |
| Client Framework (WPF) | CommunityToolkit.Mvvm | MVVM implementation |
| Server | ASP.NET Core 8 | Web API and WebSocket |
| User DB | Oracle 12c+ | User account storage |
| Message DB | MongoDB 4.0+ | Messages, chats, attachments, preferences |
| Cache | Redis 6.0+ | Session, cache, presence, rate limiting |
| Auth | JWT | Stateless authentication |
| Voice Calls (WPF) | NAudio (client) | Audio capture and playback |
| Voice Messages (web) | MediaRecorder API | Browser-native voice notes |
| Voice Signaling | WebSocket | Call signaling and control |
| Logging | Serilog | Structured logging |
| Validation | FluentValidation | Input validation |
| Testing | xUnit + FluentAssertions | Unit/integration tests |
| Serialization | System.Text.Json | High-performance JSON |
| Password | BCrypt | Secure password hashing |
| Code Editor (WPF) | AvalonEdit | Code preview |
| Local Storage (WPF) | LiteDB | Client-side storage |

---

## Future Enhancements

### Phase 2
- File and media sharing enhancements
- Video calls (WPF voice calls already implemented; web voice calls TBD)
- Message search functionality
- User blocking and reporting
- End-to-end encryption for voice calls
- Call history and recording

### Phase 3
- Native mobile wrappers (optional MAUI / PWA installability)
- WebRTC voice/video calls in the web client
- Advanced compliance features (GDPR, HIPAA)
- Multi-language support expansion

### Phase 4
- AI-powered features (chatbots, translation, summarization)
- Analytics and insights dashboard
- Custom integrations and webhooks
- Enterprise features (SSO, LDAP, Active Directory)
- Advanced moderation tools

---

## Design Principles

1. **Separation of Concerns**: Clear boundaries between layers
2. **Dependency Injection**: Loose coupling through DI
3. **Repository Pattern**: Data access abstraction
4. **MVVM Pattern**: Client-side architecture
5. **RESTful Design**: Standard HTTP methods and status codes
6. **Security First**: Security considerations at every layer
7. **Performance**: Optimized queries, caching, connection pooling
8. **Scalability**: Stateless design, horizontal scaling support
9. **Maintainability**: Clean code, comprehensive documentation
10. **Testability**: Unit tests, integration tests, testable design

---

This architecture provides a solid foundation for a scalable, secure, and maintainable real-time chat application.

