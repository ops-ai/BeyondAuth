﻿using Identity.Core;
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
        /// <param name="range">Paging range [from-to]</param>
        /// <param name="ct"></param>
        /// <response code="206">Groups information</response>
        /// <response code="500">Server error getting groups</response>
        [ProducesResponseType(typeof(IEnumerable<GroupModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromRoute] string name, [FromQuery] string sort = "+email", [FromQuery] string range = "0-19", CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var group = await session.LoadAsync<Group>($"Groups/{name}", ct);
                    var query = session.Query<ApplicationUser>().Where(t => t.Groups.Contains(name)).AsQueryable();
                    if (sort.StartsWith("-"))
                        query = query.OrderByDescending(sort[1..], Raven.Client.Documents.Session.OrderingType.String);
                    else
                        query = query.OrderBy(sort[1..], Raven.Client.Documents.Session.OrderingType.String);

                    var from = int.Parse(range.Split('-')[0]);
                    var to = int.Parse(range.Split('-')[1]) + 1;

                    return this.Partial(await query.Skip(from).Take(to - from).ToListAsync(ct)
                        .ContinueWith(t => t.Result.Select(c => new GroupMemberModel { CreatedOnUtc = group.Members[c.Id].CreatedOnUtc, DisplayName = c.DisplayName, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email, Id = c.Id.Split('/').Last() }), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
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
                    foreach (var user in users.Where(t => t.Value != null))
                    {
                        group.Members.Add(user.Value.Id, new GroupMemberInfo());
                        user.Value.Groups.Add(group.Id);
                    }

                    await session.SaveChangesAsync(ct);
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
                    foreach (var user in users.Where(t => t.Value != null))
                    {
                        group.Members.Remove(user.Value.Id);
                        user.Value.Groups.Remove(group.Id);
                    }

                    await session.SaveChangesAsync(ct);

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
