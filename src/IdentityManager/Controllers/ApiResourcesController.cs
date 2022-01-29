using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
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
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/api-resources")]
    [ApiController]
    [OpenApiTag("Api Resources", AddToDocument = true, DocumentationDescription = "APIs")]
    public class ApiResourcesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ApiResourcesController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public ApiResourcesController(IDocumentStore documentStore, ILogger<ApiResourcesController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get Api Resources
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Api Resources</response>
        /// <response code="500">Server error getting api resources</response>
        [ProducesResponseType(typeof(IEnumerable<ApiResourceModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string sort = "+name", [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<ApiResourceEntity>().AsQueryable();
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
                _logger.LogError(ex, "Error getting Api Resources");
                throw;
            }
        }

        /// <summary>
        /// Get a single API resource
        /// </summary>
        /// <param name="name"></param>
        /// <response code="200">Api Resource</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error getting Api Resource</response>
        [ProducesResponseType(typeof(ApiResourceModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(int name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
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
        /// <param name="resource"></param>
        /// <response code="204">Api Resource created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating Api Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ApiResourceModel resource)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (resource == null)
                    throw new ArgumentException("resource is required", nameof(resource));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
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
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
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
        /// <param name="model"></param>
        /// <response code="204">Api Resource updated</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error updating Api Resource</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromBody] ApiResourceModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<ApiResourceEntity>($"ApiResources/{model.Name}");
                    if (resource == null)
                        throw new KeyNotFoundException($"Resource {model.Name} was not found");

                    resource.Description = model.Description;
                    resource.DisplayName = model.DisplayName;
                    resource.Enabled = model.Enabled;
                    resource.Name = model.Name;
                    resource.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
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
                return NotFound();
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
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify an Api Resource</remarks>
        /// <response code="204">Api Resource was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Api Resource was not found</response>
        /// <response code="500">Error updating Api Resource</response>
        [HttpPatch("{name}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<ApiResourceModel> patch)
        {
            try
            {
                var originalResourceObj = await Get(name) as OkObjectResult;
                var originalResource = (ApiResourceModel)originalResourceObj.Value;

                patch.ApplyTo(originalResource);
                return await Put(originalResource);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update resource {name}.");

                return ValidationProblem(new ValidationProblemDetails { Detail = ex.FailedOperation.op });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Api Resource");
                throw;
            }
        }

        /// <summary>
        /// Delete an Api Resource
        /// </summary>
        /// <param name="name"></param>
        /// <response code="201">Api Resource deleted</response>
        /// <response code="404">Api Resource not found</response>
        /// <response code="500">Server error deleting Api Resource</response>
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
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Resource");
                throw;
            }
        }
    }
}
