namespace NexusTeam.Client.Infrastructure
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Client.Services;

    /// <summary>
    /// Message handler that adds JWT authentication token to HTTP requests.
    /// </summary>
    public class AuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IAuthenticationService authenticationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationMessageHandler"/> class.
        /// </summary>
        /// <param name="authenticationService">Authentication service.</param>
        public AuthenticationMessageHandler(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService;
        }

        /// <inheritdoc/>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Add authorization header if token is available
            var accessToken = this.authenticationService.AccessToken;
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
