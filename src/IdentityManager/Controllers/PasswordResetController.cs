using Audit.Core;
using Identity.Core;
using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using System.Net;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/users/{userId}/password-reset")]
    [ApiController]
    [OpenApiTag("Password Reset", AddToDocument = true, DocumentationDescription = "Request password reset to be sent")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;
        private readonly UserManager<ApplicationUser> _userManager;

        public PasswordResetController(IDocumentStore documentStore, ILogger<PasswordResetController> logger, IOptions<IdentityStoreOptions> identityStoreOptions, UserManager<ApplicationUser> userManager)
        {
            _documentStore = documentStore;
            _logger = logger;
            _identityStoreOptions = identityStoreOptions;
            _userManager = userManager;
        }

        /// <summary>
        /// Create new password reset request
        /// </summary>
        /// <param name="userId"></param>
        /// <response code="204">Password reset request created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error creating password reset request</response>
        [ProducesResponseType(typeof(string), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Post([FromRoute] string userId, CancellationToken ct = default)
        {
            try
            {
                if (userId == null)
                    throw new ArgumentException("userId is required", nameof(userId));

                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    _logger.LogDebug($"Creating password reset request for user {userId}");

                    var user = await _userManager.FindByIdAsync($"ApplicationUsers/{userId}");
                    if (user == null)
                        user = await _userManager.FindByEmailAsync(userId);
                    if (user == null)
                        throw new KeyNotFoundException(userId);

                    using (var audit = await AuditScope.CreateAsync("User:PasswordResetRequest", () => user, new { user.Id }))
                    {
                        var request = new PasswordResetRequest
                        {
                            UserId = userId
                        };
                        await session.StoreAsync(request, ct);

                        await session.SaveChangesAsync(ct);
                    }

                    return Ok();
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Validation error creating password reset request");
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error creating password reset request");
                return ValidationProblem(new ValidationProblemDetails { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error creating password reset request");

                throw;
            }
        }
    }
}
