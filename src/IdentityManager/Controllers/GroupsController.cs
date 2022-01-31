using Audit.Core;
using Identity.Core;
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
using Raven.Client.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/groups")]
    [ApiController]
    [OpenApiTag("Groups", AddToDocument = true, DocumentationDescription = "Groups")]
    public class GroupsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public GroupsController(IDocumentStore documentStore, ILogger<GroupsController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get groups
        /// </summary>
        /// <param name="name">Name starts with</param>
        /// <param name="tag">Contains tag</param>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="range">Paging range [from-to]</param>
        /// <param name="ct"></param>
        /// <response code="206">Groups information</response>
        /// <response code="500">Server error getting groups</response>
        [ProducesResponseType(typeof(IEnumerable<GroupModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? name = null, [FromQuery] string? tag = null, [FromQuery] string sort = "+name", [FromQuery] string range = "0-19", CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var query = session.Query<Group>().AsQueryable();
                    if (name != null)
                        query = query.Where(t => t.Name.StartsWith(name));
                    if (tag != null)
                        query = query.Where(t => t.Tags.Any(s => s.Equals(tag)));

                    if (sort.StartsWith("-"))
                        query = query.OrderByDescending(sort[1..], Raven.Client.Documents.Session.OrderingType.String);
                    else
                        query = query.OrderBy(sort[1..], Raven.Client.Documents.Session.OrderingType.String);

                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    return this.Partial(await query.Skip(from).Take(to - from).ToListAsync(ct).ContinueWith(t => t.Result.Select(c => new GroupModel {  CreatedOnUtc = c.CreatedOnUtc, Name = c.Name, Tags = c.Tags, UpdatedAt = c.UpdatedAt }), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups");
                throw;
            }
        }

        /// <summary>
        /// Get a single group by name
        /// </summary>
        /// <param name="name"></param>
        /// <response code="200">Group information</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Server error getting group</response>
        [ProducesResponseType(typeof(GroupModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet("{name}")]
        public async Task<IActionResult> Get([FromRoute]string name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var group = await session.LoadAsync<Group>($"Groups/{name}");
                    if (group == null)
                        throw new KeyNotFoundException($"Group {name} was not found");

                    return Ok(new GroupModel { CreatedOnUtc = group.CreatedOnUtc, Name = group.Name, Tags = group.Tags, UpdatedAt = group.UpdatedAt });
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Group not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group");
                throw;
            }
        }

        /// <summary>
        /// Create new group
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Group created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating group</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GroupModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (model == null)
                    throw new ArgumentException("model is required", nameof(model));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating group {model.Name}");

                    if (await session.Advanced.ExistsAsync($"Groups/{model.Name}", ct))
                        throw new ArgumentException("Group already exists");

                    Group? group = null;
                    using (var audit = await AuditScope.CreateAsync("Group:Create", () => group))
                    {
                        group = new Group
                        {
                            Name = model.Name,
                            Tags = model.Tags,
                            OwnerId = User.FindFirstValue("sub"),
                            UpdatedAt = DateTime.UtcNow
                        };

                        await session.StoreAsync(group, ct);
                        await session.SaveChangesAsync(ct);
                        audit.SetCustomField("Id", group.Id);
                    }
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating group");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating group");

                throw;
            }
        }

        /// <summary>
        /// Update a group
        /// </summary>
        /// <param name="name">Group name</param>
        /// <param name="model">Updated properties</param>
        /// <response code="204">Group updated</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Server error updating group</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("{name}")]
        public async Task<IActionResult> Put([FromRoute] string name, [FromBody] GroupModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    if (group == null)
                        throw new KeyNotFoundException($"Group {name} was not found");

                    using (var audit = await AuditScope.CreateAsync("Group:Update", () => group, new { group.Id }))
                    {
                        group.Name = model.Name;
                        group.UpdatedAt = DateTime.UtcNow;
                        group.Tags = model.Tags;

                        await session.SaveChangesAsync(ct);
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Group not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                throw;
            }
        }

        /// <summary>
        /// Update one or more properties on a group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify a group</remarks>
        /// <response code="204">Group was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">Group was not found</response>
        /// <response code="500">Error updating group</response>
        [HttpPatch("{name}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch([FromRoute] string name, [FromBody] JsonPatchDocument<GroupModel> patch, CancellationToken ct = default)
        {
            try
            {
                var originalGroupObj = await Get(name, ct: ct) as OkObjectResult;
                var originalGroup = (GroupModel)originalGroupObj!.Value!;

                patch.ApplyTo(originalGroup);
                return await Put(name, originalGroup, ct);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update group {name}.");

                return ValidationProblem(new ValidationProblemDetails { Detail = ex.FailedOperation.op });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating name");
                throw;
            }
        }

        /// <summary>
        /// Delete a group
        /// </summary>
        /// <param name="name"></param>
        /// <response code="201">Group deleted</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Server error deleting group</response>
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
                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    if (group == null)
                        throw new KeyNotFoundException($"Group {name} was not found");

                    using (var audit = await AuditScope.CreateAsync("Group:Delete", () => group, new { group.Id, MemberIds = group.Members.Keys }))
                    {
                        foreach (var userId in group.Members.Keys)
                            session.Advanced.Patch<ApplicationUser, string>(userId, t => t.Groups, g => g.RemoveAll(t => t == group.Id));

                        session.Delete(group);

                        await session.SaveChangesAsync(ct);
                        group = null;
                    }

                    return NoContent();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Group not found");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group");
                throw;
            }
        }
    }
}
