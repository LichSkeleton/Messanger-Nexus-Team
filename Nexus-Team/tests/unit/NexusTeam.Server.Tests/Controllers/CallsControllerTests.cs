namespace NexusTeam.Server.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using NexusTeam.Server.Controllers;
    using Serilog;
    using Xunit;

    public class CallsControllerTests
    {
        [Fact]
        public void GetIceServers_UsesConfiguredExternalTurnHostAndBothTransports()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TurnSecret"] = "test-turn-secret",
                    ["TurnExternalHost"] = "194.95.221.220",
                })
                .Build();
            var controller = new CallsController(
                configuration,
                new LoggerConfiguration().CreateLogger(),
                null!,
                null!)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
            controller.HttpContext.Items["UserId"] = "user-1";
            controller.HttpContext.Request.Host = new HostString("internal-proxy");

            var result = controller.GetIceServers();

            var response = Assert.IsType<CallsController.IceServersResponse>(
                Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Contains(response.IceServers, server => server.Urls.Contains("stun:194.95.221.220:3478"));
            var turnServers = response.IceServers.Where(server => server.Username != null).ToList();
            Assert.Equal(3, turnServers.Count);
            Assert.Contains(turnServers, server => server.Urls.Contains("turn:194.95.221.220:3478?transport=tcp"));
            Assert.Contains(turnServers, server => server.Urls.Contains("turn:194.95.221.220:3478?transport=udp"));
            Assert.Contains(turnServers, server => server.Urls.Contains("turn:194.95.221.220:5349?transport=tcp"));
            Assert.All(turnServers, turn =>
            {
                var usernameParts = turn.Username!.Split(':');
                Assert.Equal(2, usernameParts.Length);
                Assert.Equal("user-1", usernameParts[1]);
                Assert.True(long.TryParse(usernameParts[0], out var expires));
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                Assert.InRange(expires, now + (23 * 3600), now + (25 * 3600));
                using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes("test-turn-secret"));
                var expectedCredential = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(turn.Username)));
                Assert.Equal(expectedCredential, turn.Credential);
            });
        }
    }
}
