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
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/api-resources/{name}/secrets")]
    [ApiController]
    public class ApiResourceSecretsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ApiResourceSecretsController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public ApiResourceSecretsController(IDocumentStore documentStore, ILogger<ApiResourceSecretsController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get api resource secrets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sort"></param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Api resource secrets</response>
        /// <response code="500">Server error getting api resource</response>
        [ProducesResponseType(typeof(IEnumerable<SecretModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] string name, string sort, [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Api Resource {name} was not found");
                    
                    return this.Partial(resource.ApiSecrets.Take(to - from).Select((t, idx) => t.ToModel()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting api resource secrets");
                throw;
            }
        }

        /// <summary>
        /// Get a single api resource secret
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <response code="200">Api resource secret information</response>
        /// <response code="404">Api resource secret not found</response>
        /// <response code="500">Server error getting Api resource secret</response>
        [ProducesResponseType(typeof(SecretModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute] string name, [FromRoute] string id)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null || !resource.ApiSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Secret {id} for Api Resource {name} was not found");

                    return Ok(resource.ApiSecrets.First(t => t.Value.Sha256().Equals(id)).ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api resource secret not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting api resource secret");
                throw;
            }
        }

        /// <summary>
        /// Create new api resource secret
        /// </summary>
        /// <param name="name"></param>
        /// <param name="apiResourceSecret"></param>
        /// <response code="200">Api resource secret created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating api resource secret</response>
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string name, [FromBody] SecretModel apiResourceSecret)
        {
            try
            {
                if (apiResourceSecret == null)
                    throw new ArgumentException("apiResourceSecret is required", nameof(apiResourceSecret));

                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("name is required", nameof(name));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating api resource secret for resource {name}");

                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Api Resource {name} was not found");

                    var secret = apiResourceSecret.FromModel();
                    var newSecret = shortid.ShortId.Generate(new shortid.Configuration.GenerationOptions { Length = 32, UseNumbers = true, UseSpecialCharacters = true });
                    secret.Value = newSecret.Sha256();

                    resource.ApiSecrets.Add(secret);

                    await session.SaveChangesAsync();

                    return Ok(newSecret);
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating api resource secret");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating api resource secret");

                throw;
            }
        }

        /// <summary>
        /// Update an api resource secret
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="model"></param>
        /// <response code="204">Api resource secret updated</response>
        /// <response code="404">Api resource secret not found</response>
        /// <response code="500">Server error updating api resource secret</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] string name, [FromRoute] string id, [FromBody] SecretModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null || !resource.ApiSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Api Resource {name} was not found");

                    var secret = resource.ApiSecrets.First(t => t.Value.Sha256().Equals(id));

                    secret.Description = model.Description;
                    secret.Expiration = model.Expiration;
                    secret.Type = model.Type.ToString();

                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api resource secret not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating api resource secret");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on an api resource secret
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify an api resource secret</remarks>
        /// <response code="204">Api resource secret was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Api resource secret was not found</response>
        /// <response code="500">Error updating api resource secret</response>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(void), 204)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<IActionResult> Patch([FromRoute] string name, [FromRoute] string id, [FromBody] JsonPatchDocument<SecretModel> patch)
        {
            try
            {
                var originalSecretObj = await Get(name, id) as OkObjectResult;
                var originalSecret = (SecretModel)originalSecretObj.Value;

                patch.ApplyTo(originalSecret);
                return await Put(name, id, originalSecret);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update api resource secret {name}/{id}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating api resource secret");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }

        /// <summary>
        /// Delete an api resource secret
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <response code="201">Api resource secret deleted</response>
        /// <response code="404">Api resource secret not found</response>
        /// <response code="500">Server error deleting client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] string name, [FromRoute] string id)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null || !resource.ApiSecrets.Any(t => t.Value.Sha256().Equals(id)))
                        throw new KeyNotFoundException($"Api Resource {name} was not found");

                    var secret = resource.ApiSecrets.First(t => t.Value.Sha256().Equals(id));
                    if (!resource.ApiSecrets.Remove(secret))
                        throw new Exception("Failed to remove secret");

                    await session.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api resource secret not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting api resource secret");
                throw;
            }
        }
    }
}
