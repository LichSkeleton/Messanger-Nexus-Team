namespace NexusTeam.Client.Models
{
    using System;

    /// <summary>
    /// Server connection configuration from CLI arguments.
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// Gets or sets the server IP address or hostname.
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the server port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets the HTTP base URL.
        /// </summary>
        public string HttpBaseUrl => $"http://{this.IpAddress}:{this.Port}";

        /// <summary>
        /// Gets the WebSocket URL.
        /// </summary>
        public string WebSocketUrl => $"ws://{this.IpAddress}:{this.Port}/ws";

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns>Error message if invalid, null if valid.</returns>
        public string? Validate()
        {
            if (string.IsNullOrWhiteSpace(this.IpAddress))
            {
                return "IP address cannot be empty";
            }

            if (this.Port < 1 || this.Port > 65535)
            {
                return $"Port must be between 1 and 65535, got {this.Port}";
            }

            return null;
        }
    }
}
