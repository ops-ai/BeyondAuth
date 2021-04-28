using IdentityManager.Domain;
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
    [Route("{dataSourceId}/clients/{clientId}/secrets")]
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
        /// <param name="dataSourceId"></param>
        /// <param name="clientId"></param>
        /// <param name="sort"></param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Clients information</response>
        /// <response code="500">Server error getting clients</response>
        [ProducesResponseType(typeof(IEnumerable<ClientSecretModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] string dataSourceId, [FromRoute] string clientId, string sort, [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;
                    var query = session.Advanced.LoadStartingWithAsync<ClientSecretEntity>($"ClientSecrets/{clientId}/", null, from, to - from);
                    
                    return this.Partial(await query.ContinueWith(t => t.Result.Select(c => c.ToModel())));
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
        /// <param name="dataSourceId"></param>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <response code="200">Client information</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error getting client</response>
        [ProducesResponseType(typeof(ClientSecretModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] string dataSourceId, [FromRoute] string clientId, [FromRoute] string id)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var clientSecret = await session.LoadAsync<ClientSecretEntity>($"ClientSecrets/{clientId}/{id}");
                    if (clientSecret == null)
                        throw new KeyNotFoundException($"Client secret {clientId}/{id} was not found");

                    return Ok(clientSecret.ToModel());
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
        /// <param name="dataSourceId"></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <response code="204">Client created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string dataSourceId, [FromRoute] string clientId, [FromBody] ClientSecretModel clientSecret)
        {
            try
            {
                if (clientSecret == null)
                    throw new ArgumentException("client is required", nameof(clientSecret));

                if (string.IsNullOrEmpty(clientId))
                    throw new ArgumentException("clientId is required", nameof(clientId));

                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    _logger.LogDebug($"Creating client {clientId}");

                    if (await session.Advanced.ExistsAsync($"Clients/{clientId}"))
                        throw new ArgumentException("Client already exists");

                    await session.StoreAsync(clientSecret.FromModel(clientId));
                    await session.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating client secret");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
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
        /// <param name="dataSourceId"></param>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <response code="204">Client updated</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error updating client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] string dataSourceId, [FromRoute] string clientId, [FromRoute] string id, [FromBody] ClientSecretModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var clientSecret = await session.LoadAsync<ClientSecretEntity>($"ClientSecrets/{clientId}/{id}");
                    if (clientSecret == null)
                        throw new KeyNotFoundException($"Client {clientId} was not found");

                    clientSecret.Description = model.Description;
                    clientSecret.Expiration = model.Expiration;
                    clientSecret.Type = model.Type;

                    await session.SaveChangesAsync();

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
        /// Update one or more properties on a client
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="clientId"></param>
        /// <param name="id"></param>
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
        public async Task<IActionResult> Patch([FromRoute] string dataSourceId, [FromRoute] string clientId, [FromRoute] string id, [FromBody] JsonPatchDocument<ClientSecretModel> patch)
        {
            try
            {
                var originalClientObj = await Get(dataSourceId, clientId, id) as OkObjectResult;
                var originalClient = (ClientSecretModel)originalClientObj.Value;

                patch.ApplyTo(originalClient);
                return await Put(dataSourceId, clientId, id, originalClient);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update client secret {clientId}/{id}.");

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
        /// <param name="dataSourceId"></param>
        /// <param name="id"></param>
        /// <param name="clientId"></param>
        /// <response code="201">Client deleted</response>
        /// <response code="404">Client not found</response>
        /// <response code="500">Server error deleting client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string dataSourceId, [FromRoute] int clientId, [FromRoute] string id)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var clientsecret = await session.LoadAsync<ClientSecretEntity>($"ClientSecrets/{clientId}/{id}");
                    if (clientsecret == null)
                        throw new KeyNotFoundException($"Client secret {clientId}/{id} was not found");

                    session.Delete(clientsecret);
                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client secret not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting client secret");
                throw;
            }
        }
    }
}
