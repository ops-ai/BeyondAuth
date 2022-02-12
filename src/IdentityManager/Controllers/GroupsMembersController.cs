using Audit.Core;
using Identity.Core;
using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Net;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/groups/{name}/members")]
    [ApiController]
    [OpenApiTag("Groups", AddToDocument = true, DocumentationDescription = "Groups")]
    public class GroupsMembersController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public GroupsMembersController(IDocumentStore documentStore, ILogger<GroupsMembersController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get members
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="ct"></param>
        /// <response code="206">Groups information</response>
        /// <response code="500">Server error getting groups</response>
        [ProducesResponseType(typeof(IEnumerable<GroupModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] string name, [FromQuery] string sort = "+email", [FromQuery] int? skip = 0, [FromQuery] int? take = 20, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    var query = session.Query<ApplicationUser>().Where(t => t.Groups.Contains(name)).Statistics(out var stats).AsQueryable();
                    query = sort switch
                    {
                        "+firstName" => query.OrderBy(t => t.FirstName),
                        "-firstName" => query.OrderByDescending(t => t.FirstName),
                        "+lastName" => query.OrderBy(t => t.LastName),
                        "-lastName" => query.OrderByDescending(t => t.LastName),
                        "+email" => query.OrderBy(t => t.Email),
                        "-email" => query.OrderByDescending(t => t.Email),
                        _ => query.OrderBy(t => t.FirstName),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await query.Skip(skip??0).Take(take??20).ToListAsync(ct)
                        .ContinueWith(t => t.Result.Select(c => new GroupMemberModel { CreatedOnUtc = group.Members[c.Id!].CreatedOnUtc, DisplayName = c.DisplayName, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email, Id = c.Id!.Split('/').Last() }), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group members");
                throw;
            }
        }

        /// <summary>
        /// Add users to group
        /// </summary>
        /// <param name="model">Ids of users to add</param>
        /// <response code="204">User(s) added</response>
        /// <response code="400">Validation failed</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Server error adding users to group</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string name, [FromBody] GroupMemberAddModel model, CancellationToken ct = default)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);

                if (model == null || !model.Ids.Any())
                    throw new ArgumentException("member ids are required", nameof(model));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Adding members to group {name}");

                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    if (group == null)
                        return NotFound();

                    var users = await session.LoadAsync<ApplicationUser>(model.Ids.Select(t => $"ApplicationUsers/{t}").Except(group.Members.Keys), ct);
                    var usersToAdd = users.Where(t => t.Value != null);
                    if (!usersToAdd.Any())
                        return NoContent();

                    using (var audit = await AuditScope.CreateAsync("Group:AddMembers", () => group, new { group.Id, MemberIds = usersToAdd }))
                    {
                        foreach (var user in usersToAdd)
                        {
                            group.Members.Add(user.Value.Id!, new GroupMemberInfo());
                            session.Advanced.Patch<ApplicationUser, string>(user.Value.Id, t => t.Groups, g => g.Add(group.Id));
                        }

                        await session.SaveChangesAsync(ct);
                    }
                }

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error adding users to group");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error adding users to group");

                throw;
            }
        }

        /// <summary>
        /// Remove users from a group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="model">Ids of users to remove</param>
        /// <response code="201">Users removed</response>
        /// <response code="404">Group not found</response>
        /// <response code="500">Server error removing users from group</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete]
        public async Task<IActionResult> Delete([FromRoute] string name, [FromBody] GroupMemberAddModel model, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    if (group == null)
                        return NotFound();

                    var users = await session.LoadAsync<ApplicationUser>(model.Ids.Select(t => $"ApplicationUsers/{t}").Intersect(group.Members.Keys), ct);
                    var usersToRemove = users.Where(t => t.Value != null);
                    if (!usersToRemove.Any())
                        return NoContent();

                    using (var audit = await AuditScope.CreateAsync("Group:AddMembers", () => group, new { group.Id, MemberIds = usersToRemove }))
                    {
                        foreach (var user in usersToRemove)
                        {
                            group.Members.Remove(user.Value.Id!);
                            session.Advanced.Patch<ApplicationUser, string>(user.Value.Id, t => t.Groups, g => g.RemoveAll(t => t == group.Id));
                        }

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
                _logger.LogError(ex, "Error removing users from group");
                throw;
            }
        }
    }
}
