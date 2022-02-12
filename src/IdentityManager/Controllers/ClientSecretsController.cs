using Audit.Core;
using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/clients/{clientId}/secrets")]
    [ApiController]
    [OpenApiTag("Client Secrets", AddToDocument = true, DocumentationDescription = "Secrets associated with Clients")]
    public class ClientSecretsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ClientSecretsController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public ClientSecretsController(IDocumentStore documentStore, ILogger<ClientSecretsController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get secrets
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="sort"></param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Clients information</response>
        /// <response code="500">Server error getting clients</response>
        [ProducesResponseType(typeof(IEnumerable<SecretModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] string clientId, string sort, [FromQuery] string range = "0-19", CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    return this.Partial(client.ClientSecrets.Take(to - from).Select(t => t.ToModel()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clients");
                throw;
            }
        }

        /// <summary>
        /// Get a single client secret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <response code="200">Client secret information</response>
        /// <response code="404">Client secret not found</response>
        /// <response code="500">Server error getting client secret</response>
        [ProducesResponseType(typeof(SecretModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] string clientId, [FromRoute] string id, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null || !client.ClientSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Client secret {clientId}/{id} was not found");

                    return Ok(client.ClientSecrets.First(t => t.Value.Sha256().Equals(id)).ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client secret not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting client secret");
                throw;
            }
        }

        /// <summary>
        /// Create new client secret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <response code="204">Client secret created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating client secret</response>
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string clientId, [FromBody] SecretModel clientSecret, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (clientSecret == null)
                    throw new ArgumentException("client is required", nameof(clientSecret));

                if (string.IsNullOrEmpty(clientId))
                    throw new ArgumentException("clientId is required", nameof(clientId));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating client {clientId}");

                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    var newSecret = shortid.ShortId.Generate(new shortid.Configuration.GenerationOptions { Length = 32, UseNumbers = true, UseSpecialCharacters = true });
                    using (var audit = await AuditScope.CreateAsync("Client:AddSecret", () => client, new { client.Id }))
                    {
                        var secret = clientSecret.FromModel();
                        secret.Value = newSecret.Sha256();
                        client.ClientSecrets.Add(secret);

                        await session.SaveChangesAsync(ct);
                        audit.SetCustomField("SecretId", secret.Value);
                    }

                    return Ok(newSecret);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating client secret");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating client secret");

                throw;
            }
        }

        /// <summary>
        /// Update a client secret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <response code="204">Client secret updated</response>
        /// <response code="404">Client secret not found</response>
        /// <response code="500">Server error updating client secret</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] string clientId, [FromRoute] string id, [FromBody] SecretModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null || !client.ClientSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Client secret {clientId}/{id} was not found");

                    var secret = client.ClientSecrets.First(t => t.Value.Sha256().Equals(id));

                    using (var audit = await AuditScope.CreateAsync("Client:UpdateSecret", () => client, new { client.Id, SecretId = secret.Value }))
                    {
                        secret.Description = model.Description;
                        secret.Expiration = model.Expiration;
                        secret.Type = model.Type.ToString();

                        await session.SaveChangesAsync(ct);
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client secret not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client secret");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on a client secret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify a client</remarks>
        /// <response code="204">Client was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Client was not found</response>
        /// <response code="500">Error updating client</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch([FromRoute] string clientId, [FromRoute] string id, [FromBody] JsonPatchDocument<SecretModel> patch, CancellationToken ct = default)
        {
            try
            {
                var originalClientObj = await Get(clientId, id, ct) as OkObjectResult;
                var originalClient = (SecretModel)originalClientObj!.Value!;

                patch.ApplyTo(originalClient);
                return await Put(clientId, id, originalClient, ct);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update client secret {clientId}/{id}.");

                return ValidationProblem(new ValidationProblemDetails { Detail = ex.FailedOperation.op });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating client");
                throw;
            }
        }

        /// <summary>
        /// Delete a client secret
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <response code="201">Client secret deleted</response>
        /// <response code="404">Client secret not found</response>
        /// <response code="500">Server error deleting client secret</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string clientId, [FromRoute] string id, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var client = await session.LoadAsync<ClientEntity>($"Clients/{clientId}", ct);
                    if (client == null || !client.ClientSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Client secret {clientId}/{id} was not found");

                    var secret = client.ClientSecrets.First(t => t.Value.Sha256().Equals(id));
                    if (!client.ClientSecrets.Remove(secret))
                        throw new Exception("Failed to remove secret");
                    
                    using (var audit = await AuditScope.CreateAsync("Client:RemoveSecret", () => client, new { client.Id, SecretId = secret.Value }))
                    {
                        await session.SaveChangesAsync(ct);
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client secret not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client secret");
                throw;
            }
        }
    }
}
