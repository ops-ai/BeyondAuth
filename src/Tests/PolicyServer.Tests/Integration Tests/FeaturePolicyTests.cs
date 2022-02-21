using Autofac;
using BeyondAuth.PolicyProvider;
using Identity.Core;
using JsonSubTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using NJsonSchema.Converters;
using BeyondAuth.PolicyServer.Core.Entities;
using BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements;
using BeyondAuth.PolicyServer.Core.Models;
using PolicyServer.Tests.Fakes;
using Raven.Client.Documents;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.TestDriver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using tsh.Xunit.Logging;
using Xunit;
using Xunit.Abstractions;

namespace PolicyServer.Tests.Integration_Tests
{
    public class FeaturePolicyTests : RavenTestDriver, IClassFixture<PolicyServerWebApplicationFactory<Startup>>
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;
        private IDocumentStore _store;

        protected readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public FeaturePolicyTests(PolicyServerWebApplicationFactory<Startup> factory, ITestOutputHelper output)
        {
            _output = output;
            //ConfigureServer(new Raven.TestDriver.TestServerOptions {  });
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
                builder.ConfigureLogging(lb => lb.AddProvider(new XUnitLoggerProvider(output)));
                builder.ConfigureAppConfiguration((ctx, builder) => builder.AddInMemoryCollection(myConfiguration));
                builder.ConfigureServices(services =>
                {
                    //services.Remove(services.Single(d => d.ServiceType == typeof(IHealthCheck)));
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

        [Fact(DisplayName = "Get policies")]
        public async Task GetPolicies()
        {
            var result = _store.Maintenance.Server.Send(new GetDatabaseRecordOperation("TenantIdentity-account.beyondauth.io"));
            if (result == null)
                await _store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord("TenantIdentity-account.beyondauth.io")));

            using (var session = _store.OpenAsyncSession())
            {
                await session.StoreAsync(new TenantSetting
                {
                    Identifier = "account.beyondauth.io",
                    PolicyServerSettings = new Identity.Core.PolicyServerOptions
                    {
                        ApiName = "test"
                    }
                });
                await session.SaveChangesAsync();
            }

            using (var session = _store.OpenAsyncSession("TenantIdentity-account.beyondauth.io"))
            {
                await session.StoreAsync(new Policy
                {
                    Applicability = PolicyApplicability.Feature,
                    AuditableEvent = true,
                    ClientId = "test",
                    Criteria = new Hashtable() { { "Name", new List<string> { "Feature1", "Feature2" } } },
                    Matching = PolicyMatch.Criteria,
                    Requirements = new List<AuthorizationRequirement> { new GroupMembershipRequirement { GroupName = "Special Users" } },
                    Id = "Policies/test"
                });
                
                await session.SaveChangesAsync();
            }

            var jwt = new JwtSecurityToken("https://account.beyondauth.io", "test", new List<Claim> { new Claim("client_id", "test") }, DateTime.Now, DateTime.Now.AddHours(2));
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt.EncodedHeader + "." + jwt.EncodedPayload + ".signing");
            var response = await _client.GetAsync("policies");
            response.EnsureSuccessStatusCode();

            var srt = await response.Content.ReadAsStringAsync();
            var policies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PolicyModel>>(srt);
            
            Assert.Single(policies);
            var policy = policies[0];
            Assert.Null(policy.Name);
            Assert.Empty(policy.AuthenticationSchemes);
            Assert.Single(policy.Requirements);
            var requirement = policy.Requirements[0];
            Assert.Equal("group", requirement.Name);
            Assert.IsType<GroupMembershipRequirement>(requirement);
            Assert.Equal("Special Users", ((GroupMembershipRequirement)requirement).GroupName);

            Assert.Equal(PolicyApplicability.Feature, policy.Applicability);
            Assert.True(policy.AuditableEvent);
            Assert.Null(policy.Description);
            Assert.Equal(PolicyMatch.Criteria, policy.Matching);
            Assert.IsType<JArray>(policy.Criteria["Name"]);
            Assert.Equal("Feature1", ((JArray)policy.Criteria["Name"])[0]);
            Assert.Equal("Feature2", ((JArray)policy.Criteria["Name"])[1]);
            
            Assert.Equal(PolicyMatch.Criteria, policy.Matching);
        }

        [Fact(DisplayName = "Policy provider")]
        public async Task CheckPolicyProvider()
        {
            var result = _store.Maintenance.Server.Send(new GetDatabaseRecordOperation("TenantIdentity-account.beyondauth.io"));
            if (result == null)
                await _store.Maintenance.Server.SendAsync(new CreateDatabaseOperation(new DatabaseRecord("TenantIdentity-account.beyondauth.io")));

            using (var session = _store.OpenAsyncSession())
            {
                await session.StoreAsync(new TenantSetting
                {
                    Identifier = "account.beyondauth.io",
                    PolicyServerSettings = new Identity.Core.PolicyServerOptions
                    {
                        ApiName = "test"
                    }
                });
                await session.SaveChangesAsync();
            }

            using (var session = _store.OpenAsyncSession("TenantIdentity-account.beyondauth.io"))
            {
                await session.StoreAsync(new Policy
                {
                    Applicability = PolicyApplicability.Feature,
                    AuditableEvent = true,
                    ClientId = "test",
                    Criteria = new Hashtable() { { "Name", new List<string> { "Feature1", "Feature2" } } },
                    Matching = PolicyMatch.Criteria,
                    Requirements = new List<AuthorizationRequirement> { new GroupMembershipRequirement { GroupName = "Special Users" } },
                    Id = "Policies/test"
                });

                await session.SaveChangesAsync();
            }

            var jwt = new JwtSecurityToken("https://account.beyondauth.io", "test", new List<Claim> { new Claim("client_id", "test") }, DateTime.Now, DateTime.Now.AddHours(2));
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt.EncodedHeader + "." + jwt.EncodedPayload + ".signing");
            var response = await _client.GetAsync("policies");
            response.EnsureSuccessStatusCode();

            var srt = await response.Content.ReadAsStringAsync();
            var policies = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PolicyModel>>(srt);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var clientHandlerStub = new DelegatingHandlerStub((request, cancellationToken) => {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(srt) };
                return Task.FromResult(response);
            });
            var client = new HttpClient(clientHandlerStub);

            httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(_ => _.GetService(It.IsAny<Type>())).Returns(null);
            var loggerFactoryMock = new Mock<ILoggerFactory>();

            var policyServerOptions = Options.Create(new BeyondAuth.PolicyProvider.PolicyServerOptions { ClientId = "test", ClientSecret = "" });
            //IPolicyProvider policyProvider = new BeyondAuthPolicyProvider(policyServerOptions, httpClientFactoryMock.Object, serviceProviderMock.Object, loggerFactoryMock.Object);
            //var authPolicy = policyProvider.GetAuthorizationPolicy("policyName");

            //var featurePolicy = policyProvider.GetFeaturePolicy("Feature1");
        }
    }
}
