using Autofac;
using IdentityManager.Tests.DataManagement;
using IdentityManager.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Raven.Client.Documents;
using Raven.TestDriver;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using tsh.Xunit.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IdentityManager.Tests.Integration_Tests
{
    public class SecurityHeaderTests : RavenTestDriver, IClassFixture<AuthorizationServerWebApplicationFactory<Startup>>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private IDocumentStore _store;

        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public SecurityHeaderTests(AuthorizationServerWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _output = output;
            _store = GetDocumentStore();

            var myConfiguration = new Dictionary<string, string>
            {
                {"Raven:Urls:0", _store.Urls[0] },
                {"Raven:DatabaseName", _store.Database }
            };

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(lb => lb.AddProvider(new XUnitLoggerProvider(output)));
                builder.ConfigureAppConfiguration((ctx, builder) => builder.AddInMemoryCollection(myConfiguration));
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHealthCheck));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    services.Remove(services.First(t => t.ServiceType == typeof(IDocumentStore)));
                });
                builder.ConfigureTestServices(services =>
                 {
                     services.AddSingleton(_store);
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
            var response = await _client.GetAsync("clients");

            Assert.Contains(response.Headers, t => t.Key == "X-Content-Type-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Download-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-Frame-Options");
            Assert.Contains(response.Headers, t => t.Key == "X-XSS-Protection");
            Assert.Contains(response.Headers, t => t.Key == "Strict-Transport-Security");
        }
    }
}