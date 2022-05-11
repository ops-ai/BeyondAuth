using Audit.Core;
using BeyondAuth.Acl;
using Finbuckle.MultiTenant;
using Identity.Core;
using Identity.Core.Permissions;
using IdentityManager.Domain;
using IdentityManager.Extensions;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System.Net;

namespace IdentityManager.Controllers
{
    [Authorize("ManagePermissions")]
    [Route("{dataSourceId}/permissions")]
    [ApiController]
    [OpenApiTag("Permissions", AddToDocument = true, DocumentationDescription = "IdP Access Permissions")]
    public class PermissionsController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<PermissionsController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private readonly IAuthorizationService _authorizationService;

        public PermissionsController(IDocumentStore documentStore, ILogger<PermissionsController> logger, IOptions<IdentityStoreOptions> identityStoreOptions, IAuthorizationService authorizationService)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Get permissions
        /// </summary>
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <response code="206">Permissions information</response>
        /// <response code="500">Server error getting permissions</response>
        [ProducesResponseType(typeof(IEnumerable<ClientModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? skip = 0, [FromQuery] int? take = 20)
        {
            var tenantSetting = HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            Response.Headers.Add("X-Total-Count", tenantSetting.AceEntries.Count.ToString());
            return this.Partial(tenantSetting.AceEntries.Skip(skip ?? 0).Take(take ?? 20).Select(t => new { UserId = t.Subject.Split('/').Last(), t.AllowBits, t.DenyBits, t.IdP }));
        }

        /// <summary>
        /// Add new access control entry
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Permission added</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating client</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PermissionModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var tenantSetting = HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                _logger.LogDebug("Adding ACE {userId} {idp}", model.UserId, model.IdP);

                if (tenantSetting.AceEntries.Any(t => t.Subject == $"ApplicationUsers/{model.UserId}" && t.IdP == model.IdP))
                    throw new ArgumentException("User/IdP permission already exists");

                //make sure submitter has knowledge of idp
                //make sure user exists in idp

                var tenant = await session.LoadAsync<TenantSetting>(tenantSetting.Id, ct);
                using (var audit = await AuditScope.CreateAsync("Tenant:AddPermission", () => tenant, new { tenant.Id }))
                {
                    tenant.AceEntries.Add(new AceEntry { Subject = model.UserId, IdP = model.IdP, AllowBits = model.AllowBits, DenyBits = model.DenyBits });
                    await session.SaveChangesAsync(ct);
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Update an access control entry
        /// </summary>
        /// <param name="model"></param>
        /// <response code="204">Permission updated</response>
        /// <response code="404">Entry not found</response>
        /// <response code="400">Validation problem</response>
        /// <response code="500">Server error updating permission</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPut("")]
        public async Task<IActionResult> Put([FromBody] PermissionModel model, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var tenantSetting = HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                _logger.LogDebug("Updating ACE {userId} {idp}", model.UserId, model.IdP);

                if (!tenantSetting.AceEntries.Any(t => t.Subject == $"ApplicationUsers/{model.UserId}" && t.IdP == model.IdP))
                    return NotFound();

                var tenant = await session.LoadAsync<TenantSetting>(tenantSetting.Id, ct);
                using (var audit = await AuditScope.CreateAsync("Tenant:UpdatePermission", () => tenant, new { tenant.Id, PermissionUserId = model.UserId, model.IdP }))
                {
                    var permission = tenant.AceEntries.First(t => t.Subject == $"ApplicationUsers/{model.UserId}" && t.IdP == model.IdP);
                    permission.AllowBits = model.AllowBits;
                    permission.DenyBits = model.DenyBits;
                    await session.SaveChangesAsync(ct);
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Delete an access control entry
        /// </summary>
        /// <param name="model">User info</param>
        /// <response code="201">Entry deleted</response>
        /// <response code="404">Entry not found</response>
        /// <response code="500">Server error deleting entry</response>
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpDelete("")]
        public async Task<IActionResult> Delete(PermissionModel model, CancellationToken ct = default)
        {
            var tenantSetting = HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo!;
            using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
            {
                _logger.LogDebug("Updating ACE {userId} {idp}", model.UserId, model.IdP);

                if (!tenantSetting.AceEntries.Any(t => t.Subject == $"ApplicationUsers/{model.UserId}" && t.IdP == model.IdP))
                    return NotFound();

                var tenant = await session.LoadAsync<TenantSetting>(tenantSetting.Id, ct);
                using (var audit = await AuditScope.CreateAsync("Tenant:UpdatePermission", () => tenant, new { tenant.Id, PermissionUserId = model.UserId, model.IdP }))
                {
                    var permission = tenant.AceEntries.First(t => t.Subject == $"ApplicationUsers/{model.UserId}" && t.IdP == model.IdP);
                    tenant.AceEntries.Remove(permission);
                    await session.SaveChangesAsync(ct);
                }
            }

            return NoContent();
        }
    }
}
