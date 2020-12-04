using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IdentityServer4.Contrib.RavenDB.Tests
{
    [Collection("IdentityServer4 Tests")]
    public class RavenDBReferenceTokenStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IReferenceTokenStore _referenceTokenStore;
        private readonly IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBReferenceTokenStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _referenceTokenStore = new RavenDBReferenceTokenStore(_loggerFactory.CreateLogger<RavenDBReferenceTokenStore>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "Reference token should be retrievable after storage")]
        public async Task StoreAndGetReferenceTokenAsync()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    },
                Version = 1
            };

            var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token1);
            var token2 = await _referenceTokenStore.GetReferenceTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().NotBeNull();
            token1.ClientId.Should().Be(token2.ClientId);
            token1.Audiences.Count.Should().Be(1);
            token1.Audiences.First().Should().Be("aud");
            token1.CreationTime.Should().Be(token2.CreationTime);
            token1.Type.Should().Be(token2.Type);
            token1.Lifetime.Should().Be(token2.Lifetime);
            token1.Version.Should().Be(token2.Version);
        }

        [Fact(DisplayName = "GetReferenceTokenAsync should return null when token doesn't exist")]
        public async Task FindApiResourceAsyncNull()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    },
                Version = 1
            };

            var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token1);
            var token2 = await _referenceTokenStore.GetReferenceTokenAsync("wronghandle");

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveReferenceTokenAsync should remove token")]
        public async Task RemoveReferenceTokenAsync()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    },
                Version = 1
            };

            var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token1);
            await _referenceTokenStore.RemoveReferenceTokenAsync(handle);

            var token2 = await _referenceTokenStore.GetReferenceTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveReferenceTokensAsync should remove tokens")]
        public async Task RemoveReferenceTokensAsync()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    },
                Version = 1
            };

            var handle = await _referenceTokenStore.StoreReferenceTokenAsync(token1);
            await _referenceTokenStore.RemoveReferenceTokensAsync("123", "client");
            WaitForIndexing(_documentStore);

            var token2 = await _referenceTokenStore.GetReferenceTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBReferenceTokenStore(null, _documentStore, _identityStoreOptions));
            Assert.Throws<ArgumentException>(() => new RavenDBReferenceTokenStore(_loggerFactory.CreateLogger<RavenDBReferenceTokenStore>(), null, _identityStoreOptions));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _referenceTokenStore.GetReferenceTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _referenceTokenStore.RemoveReferenceTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _referenceTokenStore.RemoveReferenceTokensAsync(null, "client"));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _referenceTokenStore.RemoveReferenceTokensAsync("123", null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _referenceTokenStore.StoreReferenceTokenAsync(null));
        }
    }
}
