using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Extensions;
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
    public class RavenDBRefreshTokenStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly IDocumentStore _documentStore;
        protected readonly IRefreshTokenStore _tokenStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        public RavenDBRefreshTokenStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _tokenStore = new RavenDBRefreshTokenStore(_loggerFactory.CreateLogger<RavenDBRefreshTokenStore>(), _documentStore, _identityStoreOptions);
        }

        [Fact(DisplayName = "Reference token should be retrievable after storage")]
        public async Task StoreAndGetRefreshTokenAsync()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(token1);
            var token2 = await _tokenStore.GetRefreshTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().NotBeNull();
            token2.ClientId.Should().Be(token1.ClientId);
            token2.CreationTime.Should().Be(token1.CreationTime);
            token2.Lifetime.Should().Be(token1.Lifetime);
            token2.Subject.GetSubjectId().Should().Be(token1.Subject.GetSubjectId());
            token2.Version.Should().Be(token1.Version);
            token2.AccessToken.Audiences.Count.Should().Be(1);
            token2.AccessToken.Audiences.First().Should().Be("aud");
            token2.AccessToken.ClientId.Should().Be(token1.AccessToken.ClientId);
            token2.AccessToken.CreationTime.Should().Be(token1.AccessToken.CreationTime);
            token2.AccessToken.Type.Should().Be(token1.AccessToken.Type);
        }

        [Fact(DisplayName = "GetRefreshTokenAsync should return null when token doesn't exist")]
        public async Task GetRefreshTokenAsyncNull()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(token1);
            var token2 = await _tokenStore.GetRefreshTokenAsync("wronghandle");

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveRefreshTokenAsync should remove token")]
        public async Task RemoveRefreshTokenAsync()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(token1);
            await _tokenStore.RemoveRefreshTokenAsync(handle);

            var token2 = await _tokenStore.GetRefreshTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveRefreshTokenAsync should on non-existing token should skip")]
        public async Task RemoveNonExistingRefreshTokenAsync()
        {
            var exception = await Record.ExceptionAsync(() => _tokenStore.RemoveRefreshTokenAsync("invalidtoken"));
            Assert.Null(exception);

            exception = await Record.ExceptionAsync(() => _tokenStore.RemoveRefreshTokensAsync("invalidsub", "invalidclient"));
            Assert.Null(exception);
        }

        [Fact(DisplayName = "RemoveRefreshTokensAsync should remove tokens")]
        public async Task RemoveRefreshTokensAsync()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(token1);
            await _tokenStore.RemoveRefreshTokensAsync("123", "client");
            WaitForIndexing(_documentStore);

            var token2 = await _tokenStore.GetRefreshTokenAsync(handle);

            handle.Should().NotBeNull();
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "Reference token should be updated properly")]
        public async Task UpdateRefreshTokenAsync()
        {
            var token1 = new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                AccessToken = new Token
                {
                    ClientId = "client",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "foo")
                    }
                },
                Version = 1
            };

            var handle = await _tokenStore.StoreRefreshTokenAsync(token1);

            var token2 = await _tokenStore.GetRefreshTokenAsync(handle);
            token2.Lifetime = 20;
            token2.CreationTime = DateTime.UtcNow;
            token2.AccessToken = new Token
            {
                ClientId = "client2",
                Audiences = { "aud2" },
                CreationTime = DateTime.UtcNow,
                Type = "type2",
                Claims = new List<Claim>
                    {
                        new Claim("sub", "1234"),
                        new Claim("scope", "foo3")
                    }
            };

            await _tokenStore.UpdateRefreshTokenAsync(handle, token2);

            var token3 = await _tokenStore.GetRefreshTokenAsync(handle);

            handle.Should().NotBeNull();
            token3.Should().NotBeNull();
            token3.ClientId.Should().Be(token2.ClientId);
            token3.CreationTime.Should().Be(token2.CreationTime);
            token3.Lifetime.Should().Be(token2.Lifetime);
            token3.Subject.GetSubjectId().Should().Be(token2.Subject.GetSubjectId());
            token3.Version.Should().Be(token2.Version);
            token3.AccessToken.Audiences.Count.Should().Be(1);
            token3.AccessToken.Audiences.First().Should().Be("aud2");
            token3.AccessToken.ClientId.Should().Be(token2.AccessToken.ClientId);
            token3.AccessToken.CreationTime.Should().Be(token2.AccessToken.CreationTime);
            token3.AccessToken.Type.Should().Be(token2.AccessToken.Type);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _tokenStore.UpdateRefreshTokenAsync("invalidhandle", token2));
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            Assert.Throws<ArgumentException>(() => new RavenDBRefreshTokenStore(null, _documentStore, _identityStoreOptions));
            Assert.Throws<ArgumentException>(() => new RavenDBRefreshTokenStore(_loggerFactory.CreateLogger<RavenDBRefreshTokenStore>(), null, _identityStoreOptions));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.GetRefreshTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.RemoveRefreshTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.RemoveRefreshTokensAsync(null, "client"));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.RemoveRefreshTokensAsync("123", null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.StoreRefreshTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.UpdateRefreshTokenAsync(null, new RefreshToken()));
            await Assert.ThrowsAsync<ArgumentException>(async () => await _tokenStore.UpdateRefreshTokenAsync("handle", null));
        }
    }
}
