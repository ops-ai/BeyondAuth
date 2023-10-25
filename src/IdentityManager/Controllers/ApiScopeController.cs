using Audit.Core;
using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using System.Net;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/scopes")]
    [ApiController]
    [OpenApiTag("Api Scopes", AddToDocument = true, DocumentationDescription = "OAuth2/OpenID Connect Scopes")]
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
        /// <param name="sort">[-]field to sort by</param>
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <response code="206">Api scopes</response>
        /// <response code="500">Server error getting api scopes</response>
        [ProducesResponseType(typeof(IEnumerable<ApiScopeModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? sort = "name", [FromQuery] int? skip = 0, [FromQuery] int? take = 20, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<ApiScopeEntity>().Statistics(out var stats).AsQueryable();
                    query = sort switch
                    {
                        "name" => query.OrderBy(t => t.Name),
                        "-name" => query.OrderByDescending(t => t.Name),
                        _ => query.OrderBy(t => t.Name),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await query.Skip(skip ?? 0).Take(take ?? 20).ToListAsync(ct).ContinueWith(t => t.Result.Select(c => c.ToModel()), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
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
        public async Task<IActionResult> GetOne(string name, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{name}", ct);
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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<ActionResult<ApiScopeModel>> Post([FromBody] ApiScopeModel scope, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (scope == null)
                    throw new ArgumentException("scope is required", nameof(scope));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating Scope {scope.Name}");

                    if (await session.Advanced.ExistsAsync($"Scopes/{scope.Name}", ct))
                        throw new ArgumentException("Api Scope already exists");

                    ApiScopeEntity? entity = null;
                    using (var audit = await AuditScope.CreateAsync("ApiScope:Create", () => entity))
                    {
                        entity = scope.FromModel();
                        await session.StoreAsync(entity, $"Scopes/{scope.Name}", ct);
                        await session.SaveChangesAsync(ct);
                        audit.SetCustomField("ScopeId", entity.Id);
                    }
                }

                return CreatedAtRoute("Get", new { name = scope.Name }, scope);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating Api Scope");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromBody] ApiScopeModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{model.Name}", ct);
                    if (scope == null)
                        throw new KeyNotFoundException($"Scope {model.Name} was not found");

                    using (var audit = await AuditScope.CreateAsync("ApiScope:Update", () => scope, new { ScopeId = scope.Id }))
                    {
                        scope.Description = model.Description;
                        scope.DisplayName = model.DisplayName;
                        scope.Enabled = model.Enabled;
                        scope.Name = model.Name;
                        scope.Properties = model.Properties;
                        scope.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
                        scope.Required = model.Required;
                        scope.Emphasize = model.Emphasize;
                        scope.UserClaims = model.UserClaims;

                        await session.SaveChangesAsync(ct);
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Scope not found");
                return NotFound();
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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch([FromRoute] string name, [FromBody] JsonPatchDocument<ApiScopeModel> patch, CancellationToken ct = default)
        {
            try
            {
                var originalScopeObj = await GetOne(name, ct) as OkObjectResult;
                var originalScope = (ApiScopeModel)originalScopeObj!.Value!;

                patch.ApplyTo(originalScope);
                return await Put(originalScope, ct);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update scope {name}.");

                return ValidationProblem(new ValidationProblemDetails { Detail = ex.FailedOperation.op });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Api Scope");
                throw;
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
        public async Task<IActionResult> Delete(string name, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var scope = await session.LoadAsync<ApiScopeEntity>($"Scopes/{name}", ct);
                    if (scope == null)
                        throw new KeyNotFoundException($"Api Scope {name} was not found");

                    using (var audit = await AuditScope.CreateAsync("ApiScope:Delete", () => scope, new { ScopeId = scope.Id }))
                    {
                        session.Delete(scope);
                        await session.SaveChangesAsync(ct);
                        scope = null;
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Api Scope not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Api Scope");
                throw;
            }
        }
    }
}
