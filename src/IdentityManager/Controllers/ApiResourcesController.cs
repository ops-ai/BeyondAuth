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
    [Route("{dataSourceId}/api-resources")]
    [ApiController]
    public class ApiResourcesController : ControllerBase
    {

        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ApiResourcesController> _logger;

        public ApiResourcesController(IDocumentStore documentStore, ILogger<ApiResourcesController> logger)
        {
            _documentStore = documentStore;
            _logger = logger;
        }

        /// <summary>
        /// Get Api Resources
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Api Resources</response>
        /// <response code="500">Server error getting clients</response>
        [ProducesResponseType(typeof(IEnumerable<ApiResourceModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute]string dataSourceId, [FromQuery] string sort = "+name", [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var query = session.Query<ApiResourceEntity>().AsQueryable();
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
                _logger.LogError(ex, "Error getting Api Resources");
                throw;
            }
        }

        /// <summary>
        /// Get a single API resource
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="name"></param>
        /// <response code="200">Api Resource information</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error getting Api Resource</response>
        [ProducesResponseType(typeof(ApiResourceModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get([FromRoute] string dataSourceId, int name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Api Resource {name} was not found");

                    return Ok(resource.ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Resource not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Api Resource");
                throw;
            }
        }

        /// <summary>
        /// Create new Api Resource
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="resource"></param>
        /// <response code="204">Api Resource created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating Api Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string dataSourceId, [FromBody] ApiResourceModel resource)
        {
            try
            {
                if (resource == null)
                    throw new ArgumentException("resource is required", nameof(resource));

                if (string.IsNullOrEmpty(resource.Name))
                    throw new ArgumentException("name is required", nameof(resource.Name));

                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    _logger.LogDebug($"Creating Api Resource {resource.Name}");

                    if (await session.Advanced.ExistsAsync($"ApiResources/{resource.Name}"))
                        throw new ArgumentException("Api Resource already exists");

                    await session.StoreAsync(resource.FromModel(), $"ApiResources/{resource.Name}");
                    await session.SaveChangesAsync();
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating Api Resource");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating Api Resource");

                throw;
            }
        }

        /// <summary>
        /// Update an Api Resource
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="model"></param>
        /// <response code="204">Api Resource updated</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error updating Api Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromRoute] string dataSourceId, [FromBody] ApiResourceModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{model.Name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Resource {model.Name} was not found");

                    resource.Description = model.Description;
                    resource.DisplayName = model.DisplayName;
                    resource.Enabled = model.Enabled;
                    resource.Name = model.Name;
                    resource.Properties = model.Properties;
                    resource.Scopes = model.Scopes;
                    resource.UserClaims = model.UserClaims;

                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Resource not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Resource");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on an Api Resource
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify an Api Resource</remarks>
        /// <response code="204">Api Resource was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Api Resource was not found</response>
        /// <response code="500">Error updating Api Resource</response>
        [HttpPatch("{name}")]
        [ProducesResponseType(typeof(void), 204)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<IActionResult> Patch([FromRoute] string dataSourceId, string name, [FromBody] JsonPatchDocument<ApiResourceModel> patch)
        {
            try
            {
                var originalResourceObj = await Get(name) as OkObjectResult;
                var originalResource = (ApiResourceModel)originalResourceObj.Value;

                patch.ApplyTo(originalResource);
                return await Put(dataSourceId, originalResource);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update resource {name}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Api Resource");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }

        /// <summary>
        /// Delete an Api Resource
        /// </summary>
        /// <param name="dataSourceId">Datasource identifier</param>
        /// <param name="name"></param>
        /// <response code="201">Api Resource deleted</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error deleting Api Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete([FromRoute] string dataSourceId, string name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(dataSourceId))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Api Resource {name} was not found");

                    session.Delete(resource);
                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Resource not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Resource");
                throw;
            }
        }
    }
}
