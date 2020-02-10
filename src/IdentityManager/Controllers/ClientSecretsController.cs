﻿using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("api/clients/{clientId}/secrets")]
    [ApiController]
    public class ClientSecretsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ClientsController> _logger;

        public ClientSecretsController(IDocumentStore documentStore, ILogger<ClientsController> logger)
        {
            _documentStore = documentStore;
            _logger = logger;
        }

        /// <summary>
        /// Get secrets
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Clients information</response>
        /// <response code="500">Server error getting clients</response>
        [ProducesResponseType(typeof(IEnumerable<ClientModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get(string clientId, [FromQuery] string sort = "+clientName", [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var query = session.Query<ClientEntity>().AsQueryable();
                    if (sort.StartsWith("-"))
                        query = query.OrderByDescending(sort.Substring(1), Raven.Client.Documents.Session.OrderingType.String);
                    else
                        query = query.OrderBy(sort.Substring(1), Raven.Client.Documents.Session.OrderingType.String);

                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    return this.Partial(await query.Skip(from).Take(to - from).ToListAsync().ContinueWith(t => t.Result.Select(c => c.ToModel())));
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
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string clientId, string id)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}");
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
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post(string clientId, [FromBody] ClientSecretModel client)
        {
            try
            {
                if (client == null)
                    throw new ArgumentException("client is required", nameof(client));

                if (string.IsNullOrEmpty(clientId))
                    throw new ArgumentException("clientId is required", nameof(clientId));

                using (var session = _documentStore.OpenAsyncSession())
                {
                    _logger.LogDebug($"Creating client {clientId}");

                    if (await session.Advanced.ExistsAsync($"Clients/{clientId}"))
                        throw new ArgumentException("Client already exists");

                    await session.StoreAsync(client.FromModel(), $"Clients/{clientId}");
                    await session.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating client");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
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
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromBody] ClientModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{model.ClientId}");
                    if (client == null)
                        throw new KeyNotFoundException($"Client {model.ClientId} was not found");

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

                    await session.SaveChangesAsync();

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
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(void), 204)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<IActionResult> Patch(string clientId, [FromBody] JsonPatchDocument<ClientModel> patch)
        {
            try
            {
                var originalClientObj = await Get(clientId) as OkObjectResult;
                var originalClient = (ClientModel)originalClientObj.Value;

                patch.ApplyTo(originalClient);
                return await Put(originalClient);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update client {clientId}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating client");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
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
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int clientId)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}");
                    if (client == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    session.Delete(client);
                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client");
                throw;
            }
        }
    }
}