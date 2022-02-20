using Autofac;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using PolicyServer.Tests.Fakes;
using Raven.Client.Documents;
using Raven.TestDriver;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PolicyServer.Tests.Integration_Tests
{
    public class SecurityHeaderTests : RavenTestDriver, IClassFixture<PolicyServerWebApplicationFactory<Startup>>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private IDocumentStore _store;

        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public SecurityHeaderTests(PolicyServerWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _output = output;
            _store = GetDocumentStore();

            var myConfiguration = new Dictionary<string, string>
            {
                {"Raven:Urls:0", _store.Urls[0] },
                {"Raven:Database", _store.Database },
                {"LogStorage:Loki:Url", "https://test"},
                {"LogStorage:AzureStorage", " UseDevelopmentStorage=true;DevelopmentStorageProxyUri=https://127.0.0.1"},
            };

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, builder) => builder.AddInMemoryCollection(myConfiguration));
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHealthCheck));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                });
                builder.ConfigureTestServices(services =>
                 {
                     services.AddControllers(opt =>
                     {
                         opt.Filters.Add(new AllowAnonymousFilter());
                         opt.Filters.Add(new FakeUserFilter());
                     });
                 });

                builder.ConfigureTestContainer<ContainerBuilder>(services =>
                {

                });
            }).CreateClient();
        }

        [Fact(DisplayName = "Security headers are present")]
        public async Task ServerHeadersPresent()
        {
            var jwt = new JwtSecurityToken("https://test.com", "test", new List<Claim> { }, DateTime.Now, DateTime.Now.AddHours(2));
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt.EncodedHeader + "." + jwt.EncodedPayload + ".signing");

            var response = await _client.GetAsync("policies");

            Assert.Contains(response.Headers, t => t.Key == "X-Content-Type-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Download-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Frame-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-XSS-Protection");
            Assert.Contains(response.Headers, t => t.Key == "Strict-Transport-Security");
        }
    }
}