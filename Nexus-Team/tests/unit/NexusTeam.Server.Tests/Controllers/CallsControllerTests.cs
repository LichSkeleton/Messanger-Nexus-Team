namespace NexusTeam.Server.Tests.Controllers
{
    using System.Collections.Generic;
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
            var turn = Assert.Single(response.IceServers, server => server.Username != null);
            Assert.Contains("turn:194.95.221.220:3478?transport=udp", turn.Urls);
            Assert.Contains("turn:194.95.221.220:3478?transport=tcp", turn.Urls);
            Assert.Contains("turn:194.95.221.220:5349?transport=tcp", turn.Urls);
        }
    }
}
