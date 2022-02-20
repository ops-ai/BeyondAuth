using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PolicyServer.Core.Entities;
using PolicyServer.Core.Models;
using PolicyServer.Extensions;
using Raven.Client.Documents;
using System.Net;
using System.Security.Claims;

namespace PolicyServer.Controllers
{
    [ApiController]
    [Route("feature-policies")]
    [Authorize]
    public class FeaturePoliciesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public FeaturePoliciesController(IDocumentStore documentStore, ILoggerFactory factory, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = factory.CreateLogger<FeaturePoliciesController>();
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get all feature policies the app has access to
        /// </summary>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of policies to return</param>
        /// <response code="200">List of policies available to the application</response>
        /// <response code="500">Unexpected error occurred</response>
        [ProducesResponseType(typeof(IEnumerable<PolicyModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> GetAllPolicies([FromQuery] string? sort = "+name", [FromQuery] int? skip = 0, [FromQuery] int? take = 1024, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var model = new List<PolicyModel>();

                    var clientId = User.FindFirstValue("client_id");
                    var policies = session.Query<Policy>().Statistics(out var stats).Where(t => t.Applicability == PolicyApplicability.Feature && t.ClientId.Equals(clientId));
                    policies = sort switch
                    {
                        "+name" => policies.OrderBy(t => t.Name),
                        "-name" => policies.OrderByDescending(t => t.Name),
                        "+criteria" => policies.OrderBy(t => t.Criteria),
                        "-criteria" => policies.OrderByDescending(t => t.Criteria),
                        _ => policies.OrderBy(t => t.Name),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await policies.Skip(skip??0).Take(take??1024).ToListAsync(ct).ContinueWith(t => t.Result.Select(policy => new PolicyModel
                    {
                        Criteria = policy.Criteria,
                        AuthenticationSchemes = policy.AuthenticationSchemes,
                        Description = policy.Description,
                        Id = policy.Id.Split('/').Last(),
                        Name = policy.Name,
                        Requirements = policy.Requirements,
                        AuditableEvent = policy.AuditableEvent,
                        Applicability = policy.Applicability,
                        Matching = policy.Matching
                    }), ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get policies");
                throw;
            }
        }

        /// <summary>
        /// Get a policy by name
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Returns the requested policy</response>
        /// <response code="404">Policy not found</response>
        /// <response code="500">Unexpected error occurred</response>
        [ProducesResponseType(typeof(PolicyModel), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("{name}")]
        public async Task<IActionResult> GetPolicy(string name)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var clientId = User.FindFirstValue("client_id");
                    var policy = await session.Query<Policy>().FirstOrDefaultAsync(t => t.ClientId.Equals(clientId) && t.Applicability == PolicyApplicability.Feature && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (policy == null)
                        throw new KeyNotFoundException($"Policy {name} was not found");

                    var model = new PolicyModel
                    {
                        Criteria = policy.Criteria,
                        AuthenticationSchemes = policy.AuthenticationSchemes,
                        Description = policy.Description,
                        Id = policy.Id.Split('/').Last(),
                        Name = policy.Name,
                        Requirements = policy.Requirements,
                        AuditableEvent = policy.AuditableEvent,
                        Applicability = policy.Applicability,
                        Matching = policy.Matching
                    };
                    return Ok(model);
                }
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Policy not found");
                return NotFound();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get policies");
                throw;
            }
        }
    }
}
