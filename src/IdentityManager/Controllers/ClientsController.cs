using Audit.Core;
using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Net;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/clients")]
    [ApiController]
    [OpenApiTag("Clients", AddToDocument = true, DocumentationDescription = "OAuth2/OpenID Connect Clients")]
    public class ClientsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ClientsController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public ClientsController(IDocumentStore documentStore, ILogger<ClientsController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get clients
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <response code="206">Clients information</response>
        /// <response code="500">Server error getting clients</response>
        [ProducesResponseType(typeof(IEnumerable<ClientModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? sort = "+clientName", [FromQuery] int? skip = 0, [FromQuery] int? take = 20, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<ClientEntity>().Statistics(out var stats).AsQueryable();
                    query = sort switch
                    {
                        "+clientName" => query.OrderBy(t => t.ClientName),
                        "-clientName" => query.OrderByDescending(t => t.ClientName),
                        "+clientId" => query.OrderBy(t => t.ClientId),
                        "-clientId" => query.OrderByDescending(t => t.ClientId),
                        _ => query.OrderBy(t => t.ClientName),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await query.Skip(skip??0).Take(take ?? 20).ToListAsync(ct).ContinueWith(t => t.Result.Select(c => c.ToModel()), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clients");
                throw;
            }
        }

        /// <summary>
        /// Get a single client
        /// </summary>
        /// <param name="clientId"></param>
        /// <response code="200">Client information</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error getting client</response>
        [ProducesResponseType(typeof(ClientModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{clientId}")]
        public async Task<IActionResult> Get([FromRoute]string clientId, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    return Ok(client.ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client");
                throw;
            }
        }

        /// <summary>
        /// Create new client
        /// </summary>
        /// <param name="client"></param>
        /// <response code="204">Client created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ClientModel client, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (client == null)
                    throw new ArgumentException("client is required", nameof(client));

                if (string.IsNullOrEmpty(client.ClientId))
                    throw new ArgumentException("clientId is required", nameof(client.ClientId));

                if (!client.AllowedGrantTypes.All(t => t.In(new[] { GrantType.ClientCredentials, GrantType.Implicit, GrantType.Hybrid, GrantType.AuthorizationCode, GrantType.ResourceOwnerPassword, GrantType.DeviceFlow })))
                    throw new ArgumentException("value in allowedGrantTypes is not supported", nameof(client.AllowedGrantTypes));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating client {client.ClientId}");

                    if (await session.Advanced.ExistsAsync($"Clients/{client.ClientId}", ct))
                        throw new ArgumentException("Client already exists");

                    ClientEntity? entity = null;
                    using (var audit = await AuditScope.CreateAsync("Client:Create", () => entity, new { Id = $"Clients/{client.ClientId}" }))
                    {
                        entity = client.FromModel();
                        await session.StoreAsync(entity, $"Clients/{client.ClientId}", ct);
                        await session.SaveChangesAsync(ct);
                    }
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating client");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating client");

                throw;
            }
        }

        /// <summary>
        /// Update a client
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Client updated</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error updating client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{clientId}")]
        public async Task<IActionResult> Put([FromBody] ClientModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{model.ClientId}", ct);
                    if (client == null)
                        throw new KeyNotFoundException($"Client {model.ClientId} was not found");

                    if (!client.AllowedGrantTypes.All(t => t.In(new[] { GrantType.ClientCredentials, GrantType.Implicit, GrantType.Hybrid, GrantType.AuthorizationCode, GrantType.ResourceOwnerPassword, GrantType.DeviceFlow })))
                        throw new ArgumentException("value in allowedGrantTypes is not supported", nameof(client.AllowedGrantTypes));

                    using (var audit = await AuditScope.CreateAsync("Client:Update", () => client, new { client.Id }))
                    {
                        client.AbsoluteRefreshTokenLifetime = model.AbsoluteRefreshTokenLifetime;
                        client.AccessTokenLifetime = model.AccessTokenLifetime;
                        client.AccessTokenType = model.AccessTokenType;
                        client.AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser;
                        client.AllowedCorsOrigins = model.AllowedCorsOrigins;
                        client.AllowedGrantTypes = model.AllowedGrantTypes;
                        client.AllowedScopes = model.AllowedScopes;
                        client.AllowOfflineAccess = model.AllowOfflineAccess;
                        client.AllowPlainTextPkce = model.AllowPlainTextPkce;
                        client.AllowRememberConsent = model.AllowRememberConsent;
                        client.AlwaysIncludeUserClaimsInIdToken = model.AlwaysIncludeUserClaimsInIdToken;
                        client.AlwaysSendClientClaims = model.AlwaysSendClientClaims;
                        client.AuthorizationCodeLifetime = model.AuthorizationCodeLifetime;
                        client.BackChannelLogoutSessionRequired = model.BackChannelLogoutSessionRequired;
                        client.BackChannelLogoutUri = model.BackChannelLogoutUri;
                        client.Claims = model.Claims;
                        client.ClientClaimsPrefix = model.ClientClaimsPrefix;
                        client.ClientId = model.ClientId;
                        client.ClientName = model.ClientName;
                        client.ClientUri = model.ClientUri;
                        client.ConsentLifetime = model.ConsentLifetime;
                        client.Description = model.Description;
                        client.DeviceCodeLifetime = model.DeviceCodeLifetime;
                        client.Enabled = model.Enabled;
                        client.EnableLocalLogin = model.EnableLocalLogin;
                        client.FrontChannelLogoutSessionRequired = model.FrontChannelLogoutSessionRequired;
                        client.FrontChannelLogoutUri = model.FrontChannelLogoutUri;
                        client.IdentityProviderRestrictions = model.IdentityProviderRestrictions;
                        client.IdentityTokenLifetime = model.IdentityTokenLifetime;
                        client.IncludeJwtId = model.IncludeJwtId;
                        client.LogoUri = model.LogoUri;
                        client.PairWiseSubjectSalt = model.PairWiseSubjectSalt;
                        client.PostLogoutRedirectUris = model.PostLogoutRedirectUris;
                        client.Properties = model.Properties;
                        client.ProtocolType = model.ProtocolType;
                        client.RedirectUris = model.RedirectUris;
                        client.RefreshTokenExpiration = model.RefreshTokenExpiration;
                        client.RefreshTokenUsage = model.RefreshTokenUsage;
                        client.RequireClientSecret = model.RequireClientSecret;
                        client.RequireConsent = model.RequireConsent;
                        client.RequirePkce = model.RequirePkce;
                        client.SlidingRefreshTokenLifetime = model.SlidingRefreshTokenLifetime;
                        client.UpdateAccessTokenClaimsOnRefresh = model.UpdateAccessTokenClaimsOnRefresh;
                        client.UserCodeType = model.UserCodeType;
                        client.UserSsoLifetime = model.UserSsoLifetime;

                        await session.SaveChangesAsync(ct);
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating client");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on a client
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify a client</remarks>
        /// <response code="204">Client was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Client was not found</response>
        /// <response code="500">Error updating client</response>
        [HttpPatch("{clientId}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch([FromRoute] string clientId, [FromBody] JsonPatchDocument<ClientModel> patch, CancellationToken ct = default)
        {
            try
            {
                var originalClientObj = await Get(clientId, ct) as OkObjectResult;
                var originalClient = (ClientModel)originalClientObj!.Value!;

                patch.ApplyTo(originalClient);
                return await Put(originalClient, ct);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update client {clientId}.");

                return ValidationProblem(new ValidationProblemDetails { Detail = ex.FailedOperation.op });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating client");
                throw;
            }
        }

        /// <summary>
        /// Delete a client
        /// </summary>
        /// <param name="clientId"></param>
        /// <response code="201">Client deleted</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error deleting client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{clientId}")]
        public async Task<IActionResult> Delete(string clientId, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    using (var audit = await AuditScope.CreateAsync("Client:Delete", () => client, new { client.Id }))
                    {
                        session.Delete(client);
                        await session.SaveChangesAsync(ct);
                        client = null;
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client");
                throw;
            }
        }
    }
}
