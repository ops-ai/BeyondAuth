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
    [Route("{dataSourceId}/identity-resources")]
    [ApiController]
    [OpenApiTag("Identity Resources", AddToDocument = true, DocumentationDescription = "Oauth 2/OpenID Connect identity resources")]
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
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <response code="206">Identity Resources</response>
        /// <response code="500">Server error getting identity resources</response>
        [ProducesResponseType(typeof(IEnumerable<IdentityResourceModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? sort = "+name", [FromQuery] int? skip = 0, [FromQuery] int? take = 0, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<IdentityResourceEntity>().Statistics(out var stats).AsQueryable();
                    query = sort switch
                    {
                        "+name" => query.OrderBy(t => t.Name),
                        "-name" => query.OrderByDescending(t => t.Name),
                        _ => query.OrderBy(t => t.Name),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await query.Skip(skip ?? 0).Take(take ?? 20).ToListAsync(ct).ContinueWith(t => t.Result.Select(c => c.ToModel()), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
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
        public async Task<IActionResult> GetOne(string name, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{name}", ct);
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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] IdentityResourceModel resource, CancellationToken ct = default)
        {
            try
            {
                if (resource == null)
                    throw new ArgumentException("resource is required", nameof(resource));

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating Identity Resource {resource.Name}");

                    if (await session.Advanced.ExistsAsync($"IdentityResources/{resource.Name}", ct))
                        throw new ArgumentException("Identity Resource already exists");

                    IdentityResourceEntity? entity = null;
                    using (var audit = await AuditScope.CreateAsync("IdentityResource:Create", () => entity, new { ResourceId = $"IdentityResources/{resource.Name}" }))
                    {
                        entity = resource.FromModel();
                        await session.StoreAsync(entity, $"IdentityResources/{resource.Name}", ct);
                        await session.SaveChangesAsync(ct);
                    }
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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromBody] IdentityResourceModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{model.Name}", ct);
                    if (resource == null)
                        throw new KeyNotFoundException($"Resource {model.Name} was not found");

                    using (var audit = await AuditScope.CreateAsync("IdentityResource:Update", () => resource, new { ResourceId = resource.Id }))
                    {
                        resource.Description = model.Description;
                        resource.DisplayName = model.DisplayName;
                        resource.Enabled = model.Enabled;
                        resource.Name = model.Name;
                        resource.Properties = model.Properties;
                        resource.ShowInDiscoveryDocument = model.ShowInDiscoveryDocument;
                        resource.Required = model.Required;
                        resource.Emphasize = model.Emphasize;
                        resource.UserClaims = model.UserClaims;

                        await session.SaveChangesAsync(ct);
                    }

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
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch(string name, [FromBody] JsonPatchDocument<IdentityResourceModel> patch, CancellationToken ct = default)
        {
            try
            {
                var originalResourceObj = await GetOne(name, ct) as OkObjectResult;
                var originalResource = (IdentityResourceModel)originalResourceObj!.Value!;

                patch.ApplyTo(originalResource);
                return await Put(originalResource, ct);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update resource {name}.");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Identity Resource");
                throw;
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
        public async Task<IActionResult> Delete(string name, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var resource = await session.LoadAsync<IdentityResourceEntity>($"IdentityResources/{name}", ct);
                    if (resource == null)
                        throw new KeyNotFoundException($"Identity Resource {name} was not found");

                    using (var audit = await AuditScope.CreateAsync("IdentityResource:Delete", () => resource, new { ResourceId = resource.Id }))
                    {
                        session.Delete(resource);
                        await session.SaveChangesAsync(ct);
                        resource = null;
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Identity Resource not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Identity Resource");
                throw;
            }
        }
    }
}
