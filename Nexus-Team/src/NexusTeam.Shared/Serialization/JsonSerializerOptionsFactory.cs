namespace NexusTeam.Shared.Serialization
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Factory for creating pre-configured JsonSerializerOptions.
    /// Provides consistent serialization settings across the application.
    /// </summary>
    public static class JsonSerializerOptionsFactory
    {
        private static JsonSerializerOptions? defaultOptions;
        private static JsonSerializerOptions? webSocketOptions;

        /// <summary>
        /// Gets the default JsonSerializerOptions for general use.
        /// Uses camelCase naming and includes source-generated serializer context.
        /// </summary>
        public static JsonSerializerOptions Default
        {
            get
            {
                if (defaultOptions == null)
                {
                    defaultOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNameCaseInsensitive = true,
                        Converters =
                        {
                            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                        },
                        TypeInfoResolver = NexusTeamJsonSerializerContext.Default,
                    };
                }

                return defaultOptions;
            }
        }

        /// <summary>
        /// Gets JsonSerializerOptions optimized for WebSocket messages.
        /// Uses compact formatting without indentation.
        /// </summary>
        public static JsonSerializerOptions WebSocket
        {
            get
            {
                if (webSocketOptions == null)
                {
                    webSocketOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = false,
                        Converters =
                        {
                            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                        },
                        TypeInfoResolver = NexusTeamJsonSerializerContext.Default,
                    };
                }

                return webSocketOptions;
            }
        }
    }
}
