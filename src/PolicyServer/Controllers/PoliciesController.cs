using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BeyondAuth.PolicyServer.Core.Entities;
using BeyondAuth.PolicyServer.Core.Models;
using PolicyServer.Extensions;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PolicyServer.Controllers
{
    [ApiController]
    [Route("policies")]
    //[Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<PoliciesController> _logger;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public PoliciesController(IDocumentStore documentStore, ILoggerFactory factory, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _documentStore = documentStore;
            _logger = factory.CreateLogger<PoliciesController>();
            _identityStoreOptions = identityStoreOptions;
        }

        /// <summary>
        /// Get all policies the app has access to
        /// </summary>
        /// <param name="applicability">Type of policy: feature, account, authorization, password, routing, storage</param>
        /// <param name="sort">+/- field to sort by</param>
        /// <param name="skip">Number of records to skip</param>
        /// <param name="take">Number of policies to return</param>
        /// <param name="ct"></param>
        /// <response code="200">List of policies available to the application</response>
        /// <response code="500">Unexpected error occurred</response>
        [ProducesResponseType(typeof(IEnumerable<PolicyModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(500)]
        [HttpGet("")]
        [HttpGet("{applicability}")]
        public async Task<IActionResult> GetAllPolicies([FromRoute] string? applicability, [FromQuery] string? sort = "+name", [FromQuery] int? skip = 0, [FromQuery] int? take = 1024, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var model = new List<PolicyModel>();

                    var clientId = User.FindFirstValue("client_id");

                    var all = await session.Query<Policy>().ToListAsync(ct);

                    var policyQuery = session.Query<Policy>().Statistics(out var stats).Where(t => t.ClientId.Equals(clientId));
                    if (applicability != null)
                    {
                        policyQuery = applicability switch
                        {
                            "feature" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Feature),
                            "account" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Account),
                            "authorization" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Authorization),
                            "password" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Password),
                            "routing" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Routing),
                            "storage" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Storage),
                            _ => throw new NotImplementedException(),
                        };
                    }
                    policyQuery = sort switch
                    {
                        "+name" => policyQuery.OrderBy(t => t.Name),
                        "-name" => policyQuery.OrderByDescending(t => t.Name),
                        "+criteria" => policyQuery.OrderBy(t => t.Criteria),
                        "-criteria" => policyQuery.OrderByDescending(t => t.Criteria),
                        _ => policyQuery.OrderBy(t => t.Name),
                    };

                    Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                    return this.Partial(await policyQuery.Skip(skip ?? 0).Take(take ?? 1024).ToListAsync(ct).ContinueWith(t => t.Result.Select(policy => new PolicyModel
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
        [HttpGet("{applicability}/{name}")]
        public async Task<IActionResult> GetPolicy([FromRoute] string applicability, [FromRoute] string name, CancellationToken ct = default)
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                {
                    var clientId = User.FindFirstValue("client_id");
                    var policyQuery = session.Query<Policy>().Where(t => t.ClientId.Equals(clientId) && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    policyQuery = applicability switch
                    {
                        "feature" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Feature),
                        "account" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Account),
                        "authorization" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Authorization),
                        "password" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Password),
                        "routing" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Routing),
                        "storage" => policyQuery.Where(t => t.Applicability == PolicyApplicability.Storage),
                        _ => policyQuery.Where(t => t.Applicability == PolicyApplicability.Authorization)
                    };

                    var policy = await policyQuery.FirstOrDefaultAsync(ct);
                    if (policy == null)
                        throw new KeyNotFoundException($"Policy {applicability}/{name} was not found");

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
                _logger.LogError(e, "Failed to get policy");
                throw;
            }
        }
    }
}
