using Identity.Core;
using IdentityManager.Data.Audit;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/audit")]
    [ApiController]
    [OpenApiTag("Audit", AddToDocument = true, DocumentationDescription = "Audits")]
    public class AuditController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<AuditController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public AuditController(IDocumentStore documentStore, ILogger<AuditController> logger, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
        }

        [HttpGet]
        [Route("users/{id}")]
        public async Task<IActionResult> UserAudits([FromRoute] string id, CancellationToken ct = default)
        {
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                var query = session.Advanced.AsyncDocumentQuery<UserEvent>().Include(t => t.UserId).WhereEquals(t => t.SubjectId, $"ApplicationUsers/{id}");

                var events = await query.ToListAsync(ct);

                return Ok(await Task.WhenAll(events.Select(async t =>
                {
                    var user = await session.LoadAsync<ApplicationUser>(t.UserId);
                    return new { Changes = new { t.Target.New, t.Target.Old }, t.EventType, t.StartDate, User = new UserInfoModel { Id = t.UserId, Email = user?.Email, Name = $"{user?.FirstName} {user?.LastName}" } };
                })));
            }
        }

        [HttpGet]
        [Route("api-resources/{name}")]
        public async Task<IActionResult> ApiResourceAudits([FromRoute] string name, CancellationToken ct = default)
        {
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                var query = session.Advanced.AsyncDocumentQuery<ApiResourceEvent>().Include(t => t.UserId).WhereEquals(t => t.ResourceId, $"ApiResources/{name}");

                var events = await query.ToListAsync(ct);

                return Ok(await Task.WhenAll(events.Select(async t =>
                {
                    var user = await session.LoadAsync<ApplicationUser>(t.UserId);
                    return new { Changes = new { t.Target.New, t.Target.Old }, t.EventType, t.StartDate, User = new UserInfoModel { Id = t.UserId, Email = user?.Email, Name = $"{user?.FirstName} {user?.LastName}" } };
                })));
            }
        }

        [HttpGet]
        [Route("clients/{id}")]
        public async Task<IActionResult> ClientAudits([FromRoute] string id, CancellationToken ct = default)
        {
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                var query = session.Advanced.AsyncDocumentQuery<ClientEvent>().Include(t => t.UserId).WhereEquals(t => t.ClientId, $"ApplicationUsers/{id}");

                var events = await query.ToListAsync(ct);

                return Ok(await Task.WhenAll(events.Select(async t =>
                {
                    var user = await session.LoadAsync<ApplicationUser>(t.UserId);
                    return new { Changes = new { t.Target.New, t.Target.Old }, t.EventType, t.StartDate, User = new UserInfoModel { Id = t.UserId, Email = user?.Email, Name = $"{user?.FirstName} {user?.LastName}" } };
                })));
            }
        }

        [HttpGet]
        [Route("scopes/{id}")]
        public async Task<IActionResult> ScopeAudits([FromRoute] string name, CancellationToken ct = default)
        {
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                var query = session.Advanced.AsyncDocumentQuery<ScopeEvent>().Include(t => t.UserId).WhereEquals(t => t.ScopeId, $"Scopes/{name}");

                var events = await query.ToListAsync(ct);

                return Ok(await Task.WhenAll(events.Select(async t =>
                {
                    var user = await session.LoadAsync<ApplicationUser>(t.UserId);
                    return new { Changes = new { t.Target.New, t.Target.Old }, t.EventType, t.StartDate, User = new UserInfoModel { Id = t.UserId, Email = user?.Email, Name = $"{user?.FirstName} {user?.LastName}" } };
                })));
            }
        }

        [HttpGet]
        [Route("groups/{id}")]
        public async Task<IActionResult> GroupAudits([FromRoute] string id, CancellationToken ct = default)
        {
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                var query = session.Advanced.AsyncDocumentQuery<GroupEvent>().Include(t => t.UserId).WhereEquals(t => t.GroupId, $"Groups/{id}");

                var events = await query.ToListAsync(ct);

                return Ok(await Task.WhenAll(events.Select(async t =>
                {
                    var user = await session.LoadAsync<ApplicationUser>(t.UserId);
                    return new { Changes = new { t.Target.New, t.Target.Old }, t.EventType, t.StartDate, User = new UserInfoModel { Id = t.UserId, Email = user?.Email, Name = $"{user?.FirstName} {user?.LastName}" } };
                })));
            }
        }
    }
}
