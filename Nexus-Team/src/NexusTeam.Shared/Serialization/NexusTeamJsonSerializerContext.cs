namespace NexusTeam.Shared.Serialization
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using NexusTeam.Shared.Contracts;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// JSON serializer context for System.Text.Json source generation.
    /// Provides optimized serialization for all shared DTOs and contracts.
    /// Use this context for high-performance JSON operations.
    /// </summary>
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        GenerationMode = JsonSourceGenerationMode.Metadata)]
    [JsonSerializable(typeof(UserDto))]
    [JsonSerializable(typeof(ChatDto))]
    [JsonSerializable(typeof(MessageDto))]
    [JsonSerializable(typeof(StatusUpdateDto))]
    [JsonSerializable(typeof(UpdateUserStatusRequest))]
    [JsonSerializable(typeof(AvatarUpdateDto))]
    [JsonSerializable(typeof(LoginRequest))]
    [JsonSerializable(typeof(LoginResponse))]
    [JsonSerializable(typeof(RegisterRequest))]
    [JsonSerializable(typeof(AuthPayload))]
    [JsonSerializable(typeof(AuthenticateResponse))]
    [JsonSerializable(typeof(RegisterResponse))]
    [JsonSerializable(typeof(SendMessageRequest))]
    [JsonSerializable(typeof(ForwardMessageRequest))]
    [JsonSerializable(typeof(EditMessageRequest))]
    [JsonSerializable(typeof(DeleteMessageRequest))]
    [JsonSerializable(typeof(DeleteMessageNotification))]
    [JsonSerializable(typeof(ChatDeletedPayload))]
    [JsonSerializable(typeof(TypingIndicatorPayload))]
    [JsonSerializable(typeof(RateLimitErrorPayload))]
    [JsonSerializable(typeof(WebSocketMessageEnvelope))]
    [JsonSerializable(typeof(PaginatedResponse<UserDto>))]
    [JsonSerializable(typeof(PaginatedResponse<ChatDto>))]
    [JsonSerializable(typeof(PaginatedResponse<MessageDto>))]
    [JsonSerializable(typeof(ChatMessageContract))]
    [JsonSerializable(typeof(UserJoinedContract))]
    [JsonSerializable(typeof(UserLeftContract))]
    [JsonSerializable(typeof(TypingIndicatorContract))]
    [JsonSerializable(typeof(MessageDeliveredContract))]
    [JsonSerializable(typeof(MessageReadContract))]
    [JsonSerializable(typeof(UserStatusChangedContract))]
    [JsonSerializable(typeof(ErrorContract))]
    [JsonSerializable(typeof(CallRequestContract))]
    [JsonSerializable(typeof(CallAnswerContract))]
    [JsonSerializable(typeof(CallRejectContract))]
    [JsonSerializable(typeof(CallEndContract))]
    [JsonSerializable(typeof(CallSdpOfferContract))]
    [JsonSerializable(typeof(CallSdpAnswerContract))]
    [JsonSerializable(typeof(CallIceCandidateContract))]
    [JsonSerializable(typeof(CallAudioDataContract))]
    public partial class NexusTeamJsonSerializerContext : JsonSerializerContext
    {
    }
}
