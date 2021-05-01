using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
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
    [Route("{dataSourceId}/identity-resources")]
    [ApiController]
    public class IdentityResourcesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<IdentityResourcesController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public IdentityResourcesController(IDocumentStore documentStore, ILogger<IdentityResourcesController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get Identity Resources
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Identity Resources</response>
        /// <response code="500">Server error getting identity resources</response>
        [ProducesResponseType(typeof(IEnumerable<IdentityResourceModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string sort = "+name", [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<IdentityResourceEntity>().AsQueryable();
                    if (sort.StartsWith("-"))
                        query = query.OrderByDescending(sort[1..], Raven.Client.Documents.Session.OrderingType.String);
                    else
                        query = query.OrderBy(sort[1..], Raven.Client.Documents.Session.OrderingType.String);

                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    return this.Partial(await query.Skip(from).Take(to - from).ToListAsync().ContinueWith(t => t.Result.Select(c => c.ToModel())));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Identity Resources");
                throw;
            }
        }

        /// <summary>
        /// Get a single Identity resource
        /// </summary>
        /// <param name="name"></param>
        /// <response code="200">Identity Resource</response>
        /// <response code="404">Identity Resource not found</response>
        /// <response code="500">Server error getting Identity Resource</response>
        [ProducesResponseType(typeof(IdentityResourceModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(int name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Identity Resource {name} was not found");

                    return Ok(resource.ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Identity Resource not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Identity Resource");
                throw;
            }
        }

        /// <summary>
        /// Create new Identity Resource
        /// </summary>
        /// <param name="resource"></param>
        /// <response code="204">Identity Resource created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating Identity Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] IdentityResourceModel resource)
        {
            try
            {
                if (resource == null)
                    throw new ArgumentException("resource is required", nameof(resource));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating Identity Resource {resource.Name}");

                    if (await session.Advanced.ExistsAsync($"IdentityResources/{resource.Name}"))
                        throw new ArgumentException("Identity Resource already exists");

                    await session.StoreAsync(resource.FromModel(), $"IdentityResources/{resource.Name}");
                    await session.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating Identity Resource");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating Identity Resource");

                throw;
            }
        }

        /// <summary>
        /// Update an Identity Resource
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Identity Resource updated</response>
        /// <response code="404">Identity Resource not found</response>
        /// <response code="500">Server error updating Identity Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromBody] IdentityResourceModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{model.Name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Resource {model.Name} was not found");

                    resource.Description = model.Description;
                    resource.DisplayName = model.DisplayName;
                    resource.Enabled = model.Enabled;
                    resource.Name = model.Name;
                    resource.Properties = model.Properties;
                    resource.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
                    resource.Required = model.Required;
                    resource.Emphasize = model.Emphasize;
                    resource.UserClaims = model.UserClaims;

                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Identity Resource not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Identity Resource");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on an Identity Resource
        /// </summary>
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify an Identity Resource</remarks>
        /// <response code="204">Identity Resource was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Identity Resource was not found</response>
        /// <response code="500">Error updating Identity Resource</response>
        [HttpPatch("{name}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<IdentityResourceModel> patch)
        {
            try
            {
                var originalResourceObj = await Get(name) as OkObjectResult;
                var originalResource = (IdentityResourceModel)originalResourceObj.Value;

                patch.ApplyTo(originalResource);
                return await Put(originalResource);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update resource {name}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Identity Resource");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }

        /// <summary>
        /// Delete an Identity Resource
        /// </summary>
        /// <param name="name"></param>
        /// <response code="201">Identity Resource deleted</response>
        /// <response code="404">Identity Resource not found</response>
        /// <response code="500">Server error deleting Identity Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Identity Resource {name} was not found");

                    session.Delete(resource);
                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Identity Resource not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Identity Resource");
                throw;
            }
        }
    }
}
