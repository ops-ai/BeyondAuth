﻿using Autofac;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PolicyServer.Tests.Integration_Tests
{
    public class SecurityHeaderTests : IClassFixture<PolicyServerWebApplicationFactory<Startup>>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public SecurityHeaderTests(PolicyServerWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _output = output;

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHealthCheck));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                });

                builder.ConfigureTestContainer<ContainerBuilder>(services =>
                {

                });
            }).CreateClient();
        }

        [Fact(DisplayName = "Security headers are present")]
        public async Task ServerHeadersPresent()
        {
            var response = await _client.GetAsync("policies");

            response.EnsureSuccessStatusCode();

            Assert.Contains(response.Headers, t => t.Key == "X-Content-Type-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Download-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Frame-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-XSS-Protection");
            Assert.Contains(response.Headers, t => t.Key == "Strict-Transport-Security");
        }
    }
}