using Divergic.Logging.Xunit;
using FluentAssertions;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Contrib.RavenDB.Tests.Common;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Stores.Serialization;
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
    public class RavenDBPersistedGrantStoreTests : RavenIdentityServerTestBase
    {
        protected readonly ILoggerFactory _loggerFactory;

        private IPersistedGrantStore _store;
        private IAuthorizationCodeStore _codes;
        private IRefreshTokenStore _refreshTokens;
        private IReferenceTokenStore _referenceTokens;
        private IUserConsentStore _userConsent;
        private StubHandleGenerationService _handleGenerationService = new StubHandleGenerationService();
        private IDocumentStore _documentStore;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private string database = Guid.NewGuid().ToString();

        private ClaimsPrincipal _user = new IdentityServerUser("123").CreatePrincipal();

        public RavenDBPersistedGrantStoreTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _documentStore.EnsureDatabaseExists(database);
            _identityStoreOptions = Microsoft.Extensions.Options.Options.Create(new IdentityStoreOptions { DatabaseName = database });

            _store = new RavenDBPersistedGrantStore(new PersistentGrantSerializer(), _loggerFactory.CreateLogger<RavenDBPersistedGrantStore>(), _documentStore, _identityStoreOptions);

            _codes = new DefaultAuthorizationCodeStore(_store, new PersistentGrantSerializer(), _handleGenerationService, _loggerFactory.CreateLogger<DefaultAuthorizationCodeStore>());
            _refreshTokens = new DefaultRefreshTokenStore(_store, new PersistentGrantSerializer(), _handleGenerationService, _loggerFactory.CreateLogger<DefaultRefreshTokenStore>());
            _referenceTokens = new DefaultReferenceTokenStore(_store, new PersistentGrantSerializer(), _handleGenerationService, _loggerFactory.CreateLogger<DefaultReferenceTokenStore>());
            _userConsent = new DefaultUserConsentStore(_store, new PersistentGrantSerializer(), _handleGenerationService, _loggerFactory.CreateLogger<DefaultUserConsentStore>());
        }

        [Fact(DisplayName = "StoreAuthorizationCodeAsync should persist grant")]
        public async Task StoreAuthorizationCodeAsync_should_persist_grant()
        {
            var code1 = new AuthorizationCode()
            {
                ClientId = "test",
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "scope1", "scope2" }
            };

            var handle = await _codes.StoreAuthorizationCodeAsync(code1);
            WaitForIndexing(_documentStore);
            var code2 = await _codes.GetAuthorizationCodeAsync(handle);

            code1.ClientId.Should().Be(code2.ClientId);
            code1.CreationTime.Should().Be(code2.CreationTime);
            code1.Lifetime.Should().Be(code2.Lifetime);
            code1.Subject.GetSubjectId().Should().Be(code2.Subject.GetSubjectId());
            code1.CodeChallenge.Should().Be(code2.CodeChallenge);
            code1.RedirectUri.Should().Be(code2.RedirectUri);
            code1.Nonce.Should().Be(code2.Nonce);
            code1.RequestedScopes.Should().BeEquivalentTo(code2.RequestedScopes);
        }

        [Fact(DisplayName = "RemoveAuthorizationCodeAsync should remove grant")]
        public async Task RemoveAuthorizationCodeAsync_should_remove_grant()
        {
            var code1 = new AuthorizationCode()
            {
                ClientId = "test",
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "scope1", "scope2" }
            };

            var handle = await _codes.StoreAuthorizationCodeAsync(code1);
            WaitForIndexing(_documentStore);
            await _codes.RemoveAuthorizationCodeAsync(handle);
            WaitForIndexing(_documentStore);
            var code2 = await _codes.GetAuthorizationCodeAsync(handle);
            code2.Should().BeNull();
        }

        [Fact(DisplayName = "StoreRefreshTokenAsync should persist grant")]
        public async Task StoreRefreshTokenAsync_should_persist_grant()
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

            var handle = await _refreshTokens.StoreRefreshTokenAsync(token1);
            WaitForIndexing(_documentStore);
            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle);

            token1.ClientId.Should().Be(token2.ClientId);
            token1.CreationTime.Should().Be(token2.CreationTime);
            token1.Lifetime.Should().Be(token2.Lifetime);
            token1.Subject.GetSubjectId().Should().Be(token2.Subject.GetSubjectId());
            token1.Version.Should().Be(token2.Version);
            token1.AccessToken.Audiences.Count.Should().Be(1);
            token1.AccessToken.Audiences.First().Should().Be("aud");
            token1.AccessToken.ClientId.Should().Be(token2.AccessToken.ClientId);
            token1.AccessToken.CreationTime.Should().Be(token2.AccessToken.CreationTime);
            token1.AccessToken.Type.Should().Be(token2.AccessToken.Type);
        }

        [Fact(DisplayName = "RemoveRefreshTokenAsync should remove grant")]
        public async Task RemoveRefreshTokenAsync_should_remove_grant()
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

            var handle = await _refreshTokens.StoreRefreshTokenAsync(token1);
            WaitForIndexing(_documentStore);
            await _refreshTokens.RemoveRefreshTokenAsync(handle);
            WaitForIndexing(_documentStore);
            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle);
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveRefreshTokenAsync by sub and client should remove grant")]
        public async Task RemoveRefreshTokenAsync_by_sub_and_client_should_remove_grant()
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

            var handle1 = await _refreshTokens.StoreRefreshTokenAsync(token1);
            WaitForIndexing(_documentStore);
            var handle2 = await _refreshTokens.StoreRefreshTokenAsync(token1);
            WaitForIndexing(_documentStore);
            await _refreshTokens.RemoveRefreshTokensAsync("123", "client");
            WaitForIndexing(_documentStore);

            var token2 = await _refreshTokens.GetRefreshTokenAsync(handle1);
            token2.Should().BeNull();
            WaitForIndexing(_documentStore);
            token2 = await _refreshTokens.GetRefreshTokenAsync(handle2);
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "StoreReferenceTokenAsync should persist grant")]
        public async Task StoreReferenceTokenAsync_should_persist_grant()
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

            var handle = await _referenceTokens.StoreReferenceTokenAsync(token1);
            WaitForIndexing(_documentStore);
            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle);
            WaitForIndexing(_documentStore);

            token1.ClientId.Should().Be(token2.ClientId);
            token1.Audiences.Count.Should().Be(1);
            token1.Audiences.First().Should().Be("aud");
            token1.CreationTime.Should().Be(token2.CreationTime);
            token1.Type.Should().Be(token2.Type);
            token1.Lifetime.Should().Be(token2.Lifetime);
            token1.Version.Should().Be(token2.Version);
        }

        [Fact(DisplayName = "RemoveReferenceTokenAsync should remove grant")]
        public async Task RemoveReferenceTokenAsync_should_remove_grant()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle = await _referenceTokens.StoreReferenceTokenAsync(token1);
            WaitForIndexing(_documentStore);
            await _referenceTokens.RemoveReferenceTokenAsync(handle);
            WaitForIndexing(_documentStore);
            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle);
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "RemoveReferenceTokenAsync by sub and client should remove grant")]
        public async Task RemoveReferenceTokenAsync_by_sub_and_client_should_remove_grant()
        {
            var token1 = new Token()
            {
                ClientId = "client",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "foo")
                },
                Version = 1
            };

            var handle1 = await _referenceTokens.StoreReferenceTokenAsync(token1);
            WaitForIndexing(_documentStore);
            var handle2 = await _referenceTokens.StoreReferenceTokenAsync(token1);
            WaitForIndexing(_documentStore);
            await _referenceTokens.RemoveReferenceTokensAsync("123", "client");
            WaitForIndexing(_documentStore);

            var token2 = await _referenceTokens.GetReferenceTokenAsync(handle1);
            token2.Should().BeNull();
            WaitForIndexing(_documentStore);
            token2 = await _referenceTokens.GetReferenceTokenAsync(handle2);
            token2.Should().BeNull();
        }

        [Fact(DisplayName = "StoreUserConsentAsync should persist grant")]
        public async Task StoreUserConsentAsync_should_persist_grant()
        {
            var consent1 = new Consent()
            {
                CreationTime = DateTime.UtcNow,
                ClientId = "client",
                SubjectId = "123",
                Scopes = new string[] { "foo", "bar" }
            };

            await _userConsent.StoreUserConsentAsync(consent1);
            WaitForIndexing(_documentStore);
            var consent2 = await _userConsent.GetUserConsentAsync("123", "client");

            consent2.ClientId.Should().Be(consent1.ClientId);
            consent2.SubjectId.Should().Be(consent1.SubjectId);
            consent2.Scopes.Should().BeEquivalentTo(new string[] { "bar", "foo" });
        }

        [Fact(DisplayName = "RemoveUserConsentAsync should remove grant")]
        public async Task RemoveUserConsentAsync_should_remove_grant()
        {
            var consent1 = new Consent()
            {
                CreationTime = DateTime.UtcNow,
                ClientId = "client",
                SubjectId = "123",
                Scopes = new string[] { "foo", "bar" }
            };

            await _userConsent.StoreUserConsentAsync(consent1);
            WaitForIndexing(_documentStore);
            await _userConsent.RemoveUserConsentAsync("123", "client");
            WaitForIndexing(_documentStore);
            var consent2 = await _userConsent.GetUserConsentAsync("123", "client");
            consent2.Should().BeNull();
        }

        [Fact(DisplayName = "Same key for different grant types should not interfere with each other")]
        public async Task same_key_for_different_grant_types_should_not_interfere_with_each_other()
        {
            _handleGenerationService.Handle = "key";

            await _referenceTokens.StoreReferenceTokenAsync(new Token()
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2")
                }
            });

            await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 20,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
                },
                Version = 1
            });

            await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
            {
                ClientId = "client1",
                CreationTime = DateTime.UtcNow,
                Lifetime = 30,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "quux1", "quux2" }
            });

            WaitForIndexing(_documentStore);

            (await _codes.GetAuthorizationCodeAsync("key")).Lifetime.Should().Be(30);
            (await _refreshTokens.GetRefreshTokenAsync("key")).Lifetime.Should().Be(20);
            (await _referenceTokens.GetReferenceTokenAsync("key")).Lifetime.Should().Be(10);
        }

        [Fact(DisplayName = "GetAllAnyc should return all stored tokens for subject")]
        public async Task GetAllAsync()
        {
            await _referenceTokens.StoreReferenceTokenAsync(new Token()
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2")
                }
            });

            await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 20,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
                },
                Version = 1
            });

            await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
            {
                ClientId = "client1",
                CreationTime = DateTime.UtcNow,
                Lifetime = 30,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "quux1", "quux2" }
            });

            WaitForIndexing(_documentStore);

            var tokens = await _store.GetAllAsync(new PersistedGrantFilter { SubjectId = "123" });

            tokens.Should().NotBeEmpty();
            tokens.Count().Should().Be(3);
        }

        [Fact(DisplayName = "RemoveAllAsync should remove all stored tokens for subject and clientid")]
        public async Task RemoveAllAsync()
        {
            await _referenceTokens.StoreReferenceTokenAsync(new Token()
            {
                ClientId = "client1",
                Audiences = { "aud" },
                CreationTime = DateTime.UtcNow,
                Lifetime = 10,
                Type = "type",
                Claims = new List<Claim>
                {
                    new Claim("sub", "123"),
                    new Claim("scope", "bar1"),
                    new Claim("scope", "bar2")
                }
            });

            await _refreshTokens.StoreRefreshTokenAsync(new RefreshToken()
            {
                CreationTime = DateTime.UtcNow,
                Lifetime = 20,
                AccessToken = new Token
                {
                    ClientId = "client1",
                    Audiences = { "aud" },
                    CreationTime = DateTime.UtcNow,
                    Type = "type",
                    Claims = new List<Claim>
                    {
                        new Claim("sub", "123"),
                        new Claim("scope", "baz1"),
                        new Claim("scope", "baz2")
                    }
                },
                Version = 1
            });

            await _codes.StoreAuthorizationCodeAsync(new AuthorizationCode()
            {
                ClientId = "client1",
                CreationTime = DateTime.UtcNow,
                Lifetime = 30,
                Subject = _user,
                CodeChallenge = "challenge",
                RedirectUri = "http://client/cb",
                Nonce = "nonce",
                RequestedScopes = new string[] { "quux1", "quux2" }
            });

            WaitForIndexing(_documentStore);

            await _store.RemoveAllAsync(new PersistedGrantFilter { SubjectId = "123", ClientId = "client1" });

            WaitForIndexing(_documentStore);

            var tokens = await _store.GetAllAsync(new PersistedGrantFilter { SubjectId = "123" });
            tokens.Should().BeEmpty();
        }

        [Fact(DisplayName = "Parameter validation should trigger argument exceptions")]
        public async Task Validation()
        {
            using (var store = GetDocumentStore())
            {
                Assert.Throws<ArgumentException>(() => new RavenDBPersistedGrantStore(new PersistentGrantSerializer(), null, store, _identityStoreOptions));
                Assert.Throws<ArgumentException>(() => new RavenDBPersistedGrantStore(new PersistentGrantSerializer(), _loggerFactory.CreateLogger<RavenDBPersistedGrantStore>(), null, _identityStoreOptions));
                await Assert.ThrowsAsync<ArgumentException>(async () => await _store.GetAllAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await _store.GetAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await _store.RemoveAsync(null));
                await Assert.ThrowsAsync<ArgumentException>(async () => await _store.StoreAsync(null));
            }
        }
    }
}
