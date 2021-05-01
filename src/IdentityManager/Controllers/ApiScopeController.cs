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
    [Route("{dataSourceId}/scopes")]
    [ApiController]
    public class ApiScopesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<ApiScopesController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public ApiScopesController(IDocumentStore documentStore, ILogger<ApiScopesController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get Api Scopes
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <response code="206">Api scopes</response>
        /// <response code="500">Server error getting api scopes</response>
        [ProducesResponseType(typeof(IEnumerable<ApiScopeModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string sort = "+name", [FromQuery] string range = "0-19")
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<ApiScopeEntity>().AsQueryable();
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
                _logger.LogError(ex, "Error getting api scopes");
                throw;
            }
        }

        /// <summary>
        /// Get a single Api Scope
        /// </summary>
        /// <param name="name"></param>
        /// <response code="200">Api Scope</response>
        /// <response code="404">Api Scope not found</response>
        /// <response code="500">Server error getting Api Scope</response>
        [ProducesResponseType(typeof(ApiScopeModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get(int name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{name}");
                    if (scope == null)
                        throw new KeyNotFoundException($"Api Scope {name} was not found");

                    return Ok(scope.ToModel());
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Scope not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Api Scope");
                throw;
            }
        }

        /// <summary>
        /// Create new Api Scope
        /// </summary>
        /// <param name="scope"></param>
        /// <response code="201">Api Scope created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating Api Scope</response>
        [ProducesResponseType(typeof(ApiScopeModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(Dictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<ActionResult<ApiScopeModel>> Post([FromBody] ApiScopeModel scope)
        {
            try
            {
                if (scope == null)
                    throw new ArgumentException("scope is required", nameof(scope));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating Scope {scope.Name}");

                    if (await session.Advanced.ExistsAsync($"Scopes/{scope.Name}"))
                        throw new ArgumentException("Api Scope already exists");

                    await session.StoreAsync(scope.FromModel(), $"Scopes/{scope.Name}");
                    await session.SaveChangesAsync();
                }

                return CreatedAtRoute("Get", new { name = scope.Name }, scope);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating Api Scope");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating Api Scope");

                throw;
            }
        }

        /// <summary>
        /// Update an Api Scope
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Api Scope updated</response>
        /// <response code="404">Api Scope not found</response>
        /// <response code="500">Server error updating Api Scope</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromBody] ApiScopeModel model)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{model.Name}");
                    if (scope == null)
                        throw new KeyNotFoundException($"Scope {model.Name} was not found");

                    scope.Description = model.Description;
                    scope.DisplayName = model.DisplayName;
                    scope.Enabled = model.Enabled;
                    scope.Name = model.Name;
                    scope.Properties = model.Properties;
                    scope.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
                    scope.Required = model.Required;
                    scope.Emphasize = model.Emphasize;
                    scope.UserClaims = model.UserClaims;

                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Scope not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Scope");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on an Api Scope
        /// </summary>
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify an Api Scope</remarks>
        /// <response code="204">Api Scope was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Api Scope was not found</response>
        /// <response code="500">Error updating Api Scope</response>
        [HttpPatch("{name}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<ApiScopeModel> patch)
        {
            try
            {
                var originalScopeObj = await Get(name) as OkObjectResult;
                var originalScope = (ApiScopeModel)originalScopeObj.Value;

                patch.ApplyTo(originalScope);
                return await Put(originalScope);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update scope {name}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Api Scope");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }

        /// <summary>
        /// Delete an Api Scope
        /// </summary>
        /// <param name="name"></param>
        /// <response code="201">Api Scope deleted</response>
        /// <response code="404">Api Scope not found</response>
        /// <response code="500">Server error deleting Api Scope</response>
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
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{name}");
                    if (scope == null)
                        throw new KeyNotFoundException($"Api Scope {name} was not found");

                    session.Delete(scope);
                    await session.SaveChangesAsync();

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Scope not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Scope");
                throw;
            }
        }
    }
}
