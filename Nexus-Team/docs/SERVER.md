# NexusTeam Server - Complete Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Project Structure](#project-structure)
4. [Controllers](#controllers)
5. [Services](#services)
6. [Repositories](#repositories)
7. [Middleware](#middleware)
8. [Configuration](#configuration)
9. [Database Integration](#database-integration)
10. [WebSocket Implementation](#websocket-implementation)
11. [Authentication & Authorization](#authentication--authorization)
12. [API Endpoints](#api-endpoints)
13. [Error Handling](#error-handling)
14. [Logging](#logging)
15. [Testing](#testing)
16. [Deployment](#deployment)

---

## Overview

NexusTeam.Server is an ASP.NET Core 8.0 Web API application that provides the backend services for the NexusTeam real-time chat application. It implements RESTful APIs, WebSocket support for real-time messaging, JWT authentication, and integrates with Oracle, MongoDB, and Redis databases.

### Key Features

- **RESTful API**: 8 controllers with comprehensive endpoints
- **WebSocket Support**: Real-time bidirectional communication for messaging and voice call signaling
- **JWT Authentication**: Stateless authentication with refresh tokens
- **Multi-Database**: Oracle (users), MongoDB (messages/chats/attachments/preferences), Redis (cache/sessions)
- **Input Validation**: FluentValidation for all requests
- **Rate Limiting**: Protection against abuse
- **Security Headers**: Comprehensive security headers
- **Swagger Documentation**: API documentation with Swagger/OpenAPI
- **Structured Logging**: Serilog integration

### Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Authentication**: JWT (System.IdentityModel.Tokens.Jwt)
- **Password Hashing**: BCrypt.Net-Next
- **Validation**: FluentValidation
- **Databases**: Oracle.ManagedDataAccess.Core, MongoDB.Driver, StackExchange.Redis
- **Logging**: Serilog
- **API Docs**: Swashbuckle.AspNetCore (Swagger)
- **Image Processing**: SixLabors.ImageSharp

---

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────┐
│         HTTP Request / WebSocket            │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Middleware Pipeline                 │
│  - Security Headers                          │
│  - CORS                                      │
│  - WebSocket                                 │
│  - JWT Authentication                        │
│  - Exception Handling                       │
│  - Request Logging                          │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Controllers (8)                      │
│  - AuthController                           │
│  - UsersController                          │
│  - ChatsController                          │
│  - MessagesController                        │
│  - AttachmentsController                     │
│  - PreferencesController                     │
│  - ChatFoldersController                    │
│  - GeneratedImagesController                │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Services Layer                       │
│  - Business Logic                           │
│  - Data Validation                          │
│  - WebSocket Management                     │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Repositories Layer                   │
│  - Oracle Repositories                       │
│  - MongoDB Repositories                     │
│  - Redis Services                           │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│         Databases                            │
│  - Oracle DB                                 │
│  - MongoDB                                   │
│  - Redis                                     │
└──────────────────────────────────────────────┘
```

### Design Patterns

- **MVC/API Pattern**: Controllers handle HTTP requests
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: Loose coupling
- **Middleware Pipeline**: Request/response processing
- **Service Layer**: Business logic separation
- **Observer Pattern**: WebSocket broadcasting

---

## Project Structure

```
NexusTeam.Server/
├── Configuration/           # Configuration classes
│   ├── Options/            # Configuration options
│   └── Validation/         # Option validators
├── Controllers/            # API controllers (8)
├── Data/                   # Data access layer
│   ├── Models/            # Data models
│   ├── Repositories/      # Repository interfaces
│   │   ├── OracleImpl/    # Oracle implementations
│   │   └── MongoImpl/     # MongoDB implementations
│   └── OracleDataContext.cs
├── Extensions/             # Extension methods
├── Middleware/             # Custom middleware
├── Models/                 # Response models
├── Services/               # Business services
│   └── Abstractions/      # Service interfaces
├── Validators/             # FluentValidation validators
├── Program.cs             # Application entry point
└── appsettings.json       # Configuration file
```

---

## Controllers

### 1. AuthController

**Route**: `/api/auth`

**Endpoints**:
- `POST /register` - Register new user
- `POST /login` - User login (returns JWT tokens)
- `POST /logout` - Logout and invalidate tokens
- `POST /refresh` - Refresh access token

**Features**:
- Rate limiting on login (5 attempts per 5 minutes)
- Input validation with FluentValidation
- BCrypt password hashing
- JWT token generation
- Refresh token management
- Session management

**Request/Response Examples**:

```json
// Register Request
{
  "username": "john.doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "displayName": "John Doe"
}

// Login Request
{
  "usernameOrEmail": "john.doe",
  "password": "SecurePass123!"
}

// Login Response
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 3600,
  "user": { ... }
}
```

### 2. UsersController

**Route**: `/api/users`

**Endpoints**:
- `GET /{id}` - Get user by ID
- `PUT /{id}` - Update user profile
- `GET /` - Search users
- `POST /{id}/avatar` - Upload avatar image

**Features**:
- User profile management
- Avatar image upload and processing
- User search functionality
- Profile update validation

### 3. ChatsController

**Route**: `/api/chats`

**Endpoints**:
- `GET /` - Get user's chats
- `POST /` - Create new chat
- `GET /{id}` - Get chat details
- `PUT /{id}` - Update chat
- `DELETE /{id}` - Delete chat
- `POST /{id}/participants` - Add participants
- `DELETE /{id}/participants/{userId}` - Remove participant

**Features**:
- Chat creation (direct, group, channel)
- Participant management
- Chat metadata updates
- Real-time updates via WebSocket

### 4. MessagesController

**Route**: `/api/messages` and `/api/chats/{id}/messages`

**Endpoints**:
- `POST /chats/{id}/messages` - Send message
- `GET /chats/{id}/messages` - Get message history (paginated)
- `PUT /{id}` - Edit message
- `DELETE /{id}` - Delete message

**Features**:
- Message sending with validation
- Paginated message history
- Message editing with audit trail
- Soft delete with audit log
- Real-time delivery via WebSocket

### 5. AttachmentsController

**Route**: `/api/attachments`

**Endpoints**:
- `POST /` - Upload file attachment
- `GET /{id}` - Download file attachment
- `DELETE /{id}` - Delete attachment

**Features**:
- File upload with size limits
- File type validation
- Secure file storage
- File download with proper headers

### 6. PreferencesController

**Route**: `/api/preferences`

**Endpoints**:
- `GET /` - Get user preferences
- `PUT /` - Update user preferences

**Features**:
- Theme preferences (light/dark)
- Language settings
- Notification preferences
- Privacy settings
- Stored in MongoDB

### 7. ChatFoldersController

**Route**: `/api/chat-folders`

**Endpoints**:
- `GET /` - Get user's chat folders
- `POST /` - Create folder
- `PUT /{id}` - Update folder
- `DELETE /{id}` - Delete folder

**Features**:
- Chat organization
- Folder hierarchy
- Stored in MongoDB

### 8. GeneratedImagesController

**Route**: `/api/generated-images`

**Endpoints**:
- `GET /` - Get user's generated images
- `POST /` - Generate image
- `DELETE /{id}` - Delete image

**Features**:
- AI image generation
- Image storage and management
- User-specific image gallery

---

## Services

### Core Business Services

#### AuthService
- User registration
- User authentication
- Password validation
- User profile management

#### ChatService
- Chat creation and management
- Participant management
- Chat metadata operations

#### MessageService
- Message sending and retrieval
- Message editing and deletion
- Message history pagination
- Audit trail management

#### AttachmentService
- File upload handling
- File storage management
- File download processing

#### GeneratedImageService
- AI image generation
- Image storage
- Image gallery management

#### ChatFolderService
- Folder creation and management
- Chat organization

#### AvatarService
- Avatar image processing
- Image optimization
- Avatar storage

### Infrastructure Services

#### JwtTokenService
- JWT token generation
- Token validation
- Token claims management

#### RefreshTokenService
- Refresh token generation
- Token rotation
- Token revocation

#### WebSocketConnectionManager
- WebSocket connection management
- User-to-connection mapping
- Message broadcasting
- Connection health monitoring

#### UserStatusService
- User presence tracking
- Status updates (online/offline/away/DND)
- Last seen tracking

#### SessionService
- Session management
- Session storage in Redis
- Session expiration

#### RateLimitService
- Rate limiting implementation
- Redis-based counters
- Sliding window algorithm

#### RedisCacheService
- Caching operations
- Cache invalidation
- TTL management

#### PresenceTrackingService (Background Service)
- Automatic presence updates
- Connection monitoring
- Status synchronization

### Utility Services

#### SystemClock
- Time abstraction for testing
- UTC time operations

#### UlidGenerator
- ULID generation for IDs
- Sortable unique identifiers

#### BcryptPasswordHasher
- Password hashing
- Password verification

---

## Repositories

### Oracle Repositories

#### OracleUserRepository
- User CRUD operations
- User search
- Profile management

### MongoDB Repositories

#### MongoChatRepository
- Chat operations
- Participant management
- Chat queries

#### MongoMessageRepository
- Message operations
- Message history
- Message queries

#### MongoMessageAttachmentRepository
- Attachment metadata
- File information

#### MongoUserPreferenceRepository
- User preferences
- Preference updates

#### MongoChatFolderRepository
- Folder operations
- Chat organization

#### MongoGeneratedImageRepository
- Generated image storage
- Image metadata

---

## Middleware

### 1. SecurityHeadersMiddleware

**Purpose**: Adds security headers to all HTTP responses

**Headers Added**:
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- X-XSS-Protection: 1; mode=block
- Referrer-Policy: strict-origin-when-cross-origin
- Permissions-Policy
- Content-Security-Policy

### 2. JwtAuthenticationMiddleware

**Purpose**: Validates JWT tokens and sets user context

**Features**:
- Token extraction from Authorization header
- Token validation (signature, expiration, issuer, audience)
- User context injection
- Token blacklist checking

### 3. WebSocketHandler

**Purpose**: Handles WebSocket connections for messaging and voice call signaling

**Features**:
- Connection establishment
- Authentication via JWT
- Message routing for real-time messaging
- Voice call message routing (call requests, answers, signaling)
- Connection management
- Heartbeat handling
- Call message forwarding between users

### 4. ExceptionHandlingMiddleware

**Purpose**: Global exception handling

**Features**:
- Exception catching
- Error response formatting
- Logging
- Status code mapping

### 5. RequestLoggingMiddleware

**Purpose**: Request/response logging

**Features**:
- Request logging
- Response logging
- Performance metrics
- Error logging

---

## Configuration

### appsettings.json Structure

```json
{
  "ServerConfiguration": {
    "Port": "CONFIGURED_FROM_CLI_ARGUMENTS"
  },
  "Serilog": { ... },
  "Oracle": {
    "ConnectionString": "...",
    "CommandTimeout": 30,
    "MaxRetryAttempts": 3
  },
  "MongoDB": {
    "ConnectionString": "...",
    "DatabaseName": "NexusTeam",
    "ConnectionTimeout": 10,
    "ServerSelectionTimeout": 5
  },
  "Redis": {
    "ConnectionString": "...",
    "DefaultDatabase": -1,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "NexusTeamServer",
    "Audience": "NexusTeamClient",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Bcrypt": {
    "WorkFactor": 11
  },
  "RateLimit": {
    "LoginMaxAttempts": 5,
    "LoginWindowSeconds": 300,
    "MessageMaxAttempts": 60,
    "MessageWindowSeconds": 60
  },
  "Cors": {
    "AllowedOrigins": [ ... ],
    "AllowCredentials": true,
    "PolicyName": "NexusTeamCorsPolicy"
  }
}
```

### Configuration Options

All configuration options have validators to ensure correctness at startup:
- OracleOptionsValidator
- MongoOptionsValidator
- RedisOptionsValidator
- JwtOptionsValidator
- BcryptOptionsValidator
- RateLimitOptionsValidator
- CorsOptionsValidator

---

## Database Integration

### Oracle Database

**Connection**: Managed via Oracle.ManagedDataAccess.Core

**Purpose**: Stores user account information and authentication data

**Operations**:
- User data (CRUD)
- Connection pooling
- Transaction support
- Parameterized queries

**Tables Used**:
- **users** - User account information (id, username, email, password_hash, display_name, avatar_url, status, created_at, updated_at, last_seen_at)

### MongoDB

**Connection**: Managed via MongoDB.Driver

**Purpose**: Stores messages, chats, attachments, preferences, and other flexible data

**Operations**:
- Message storage and retrieval
- Chat management
- Attachment metadata
- User preferences
- Chat folders
- Generated images
- Document queries
- Index management

**Collections Used**:
- **messages** - Message storage with reactions and threading
- **chats** - Chat/conversation storage with participants
- **attachments** - File attachment metadata
- **user_preferences** - User preferences and settings
- **chat_folders** - Chat folder organization
- **generated_images** - AI-generated image storage

### Redis

**Connection**: Managed via StackExchange.Redis

**Operations**:
- Session storage
- User presence
- Caching
- Rate limiting counters
- WebSocket connection tracking

**Key Patterns**:
- `NexusTeam:session:{sessionId}`
- `NexusTeam:presence:{userId}`
- `NexusTeam:ws:{userId}:{connectionId}`
- `NexusTeam:ratelimit:{userId}:{endpoint}`
- `NexusTeam:cache:{type}:{id}`
- `NexusTeam:refresh:{userId}:{tokenId}`

---

## WebSocket Implementation

### Connection Flow

1. Client establishes WebSocket connection to `/ws`
2. Client sends authentication message with JWT token
3. Server validates token and associates connection with user
4. Server subscribes client to user's chat channels
5. Bidirectional message exchange
6. Heartbeat to keep connection alive
7. Automatic cleanup on disconnect

### Message Types

**Messaging Contracts**:
- `UserJoinedContract` - User joined chat
- `UserLeftContract` - User left chat
- `UserStatusChangedContract` - Status update
- `MessageSentContract` - New message
- `MessageEditedContract` - Message edited
- `MessageDeletedContract` - Message deleted
- `TypingIndicatorContract` - Typing indicator
- `MessageDeliveredContract` - Delivery receipt
- `MessageReadContract` - Read receipt
- `ErrorContract` - Error notification

**Voice Call Contracts**:
- `CallRequestContract` - Initiate voice call
- `CallAnswerContract` - Accept incoming call
- `CallRejectContract` - Reject incoming call
- `CallEndContract` - End active call
- `CallSdpOfferContract` - WebRTC SDP offer for signaling
- `CallSdpAnswerContract` - WebRTC SDP answer for signaling
- `CallIceCandidateContract` - WebRTC ICE candidate exchange
- `CallAudioDataContract` - Audio data streaming during active call

### Broadcasting

- Messages broadcast to all participants in a chat
- Presence updates broadcast to relevant users
- Status changes broadcast to contacts
- Call requests forwarded to recipient
- Call signaling messages forwarded between call participants

---

## Authentication & Authorization

### JWT Authentication

**Token Generation**:
- Access tokens: 60 minutes
- Refresh tokens: 7 days
- Signed with HMAC-SHA256
- Includes user claims (sub, username, email)

**Token Validation**:
- Signature verification
- Expiration check
- Issuer/audience validation
- Blacklist check

### Authorization

**Resource-Based**:
- Users can only access their own data
- Chat participants can access chat
- Message authors can edit/delete
- Chat owners can manage participants

---

## API Endpoints

### Complete Endpoint List

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed endpoint documentation.

### Swagger Documentation

In development mode, Swagger UI is available at `/swagger`:
- Interactive API documentation
- JWT Bearer authentication support
- Request/response examples
- Try-it-out functionality

---

## Error Handling

### Error Response Format

```json
{
  "error": "Error message",
  "details": "Additional details (optional)",
  "statusCode": 400
}
```

### Validation Errors

```json
{
  "error": "Validation failed",
  "errors": {
    "username": ["Username is required"],
    "email": ["Invalid email format"]
  },
  "statusCode": 400
}
```

### Exception Handling

- Global exception handler
- Appropriate HTTP status codes
- Secure error messages (no sensitive data)
- Structured error responses

---

## Logging

### Serilog Configuration

**Log Levels**:
- Error: Exceptions and critical issues
- Warning: Validation failures, rate limiting
- Information: API requests, WebSocket events
- Debug: Detailed execution flow (dev only)

**Sinks**:
- Console (development)
- File (production)
- Future: Application Insights, ELK stack

### Logged Events

- API requests/responses
- Authentication events
- WebSocket connections
- Database operations
- Errors and exceptions
- Security events

---

## Testing

### Unit Tests

- Service tests
- Repository tests
- Validator tests
- Utility tests

### Integration Tests

- API endpoint tests
- WebSocket tests
- Database integration tests

### Test Framework

- xUnit
- FluentAssertions
- NSubstitute (mocking)

---

## Deployment

### Docker Infrastructure Setup

Before running the server, ensure Docker services are running:

```bash
# Start all Docker services
cd Nexus-Team
docker-compose up -d

# Verify services are healthy
docker-compose ps

# Check database seeder completed
docker-compose logs db-seeder
```

**Required Services:**
- Oracle: `localhost:1530`
- MongoDB: `localhost:27018`
- Redis: `localhost:6380`

### Running the Server

```powershell
# Development (with Docker)
# Ensure Docker services are running first
dotnet run 5251

# Production
dotnet publish -c Release
.\bin\Release\net8.0\nexusteam_server.exe 5251
```

### Configuration

The `appsettings.json` is pre-configured for Docker:

```json
{
  "Oracle": {
    "ConnectionString": "User Id=nexusteam_admin;Password=060707;Data Source=localhost:1530/FREEPDB1"
  },
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27018",
    "DatabaseName": "NexusTeam"
  },
  "Redis": {
    "ConnectionString": "localhost:6380"
  }
}
```

**No configuration changes needed for Docker setup!**

### Command-Line Arguments

- Port number (required): `nexusteam_server.exe <port>`

### Environment Variables

For production, you can override configuration using environment variables:
- `ORACLE_CONNECTION_STRING` - Oracle connection string
- `MONGODB_CONNECTION_STRING` - MongoDB connection string
- `REDIS_CONNECTION_STRING` - Redis connection string
- `JWT_SECRET_KEY` - JWT secret key
- Other sensitive configuration

### Production Considerations

- **Docker Services**: Ensure all Docker containers are running and healthy
- **HTTPS/TLS certificates**: Configure SSL/TLS for production
- **Environment-specific configuration**: Use environment variables or separate config files
- **Logging configuration**: Configure file logging for production
- **Monitoring setup**: Set up application monitoring
- **Health checks**: Use `/health` endpoints for container orchestration
- **Load balancing**: Configure load balancer for multiple server instances
- **Database Authentication**: Enable authentication for MongoDB and Redis in production
- **Resource Limits**: Set appropriate CPU and memory limits for Docker containers

---

## Performance Optimization

### Database

- Connection pooling
- Indexed queries
- Query optimization
- Caching strategy

### Caching

- Redis caching for frequently accessed data
- Cache-aside pattern
- TTL-based expiration
- Cache invalidation

### Connection Management

- Database connection pools
- Redis connection multiplexing
- HTTP client reuse
- WebSocket connection limits

---

This documentation provides a comprehensive overview of the NexusTeam Server implementation. For specific implementation details, refer to the source code and inline documentation.

