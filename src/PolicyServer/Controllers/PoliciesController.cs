using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PolicyServer.Core.Entities;
using PolicyServer.Models;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PolicyServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(IDocumentStore documentStore, ILoggerFactory factory)
        {
            _documentStore = documentStore;
            _logger = factory.CreateLogger<PoliciesController>();
        }

        /// <summary>
        /// Get all policies the app has access to
        /// </summary>
        /// <returns></returns>
        /// <response code="200">List of policies available to the application</response>
        /// <response code="500">Unexpected error occurred</response>
        [ProducesResponseType(typeof(List<PolicyModel>), 200)]
        [ProducesResponseType(500)]
        [HttpGet]
        public async Task<IActionResult> GetAllPolicies()
        {
            try
            {
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var model = new List<PolicyModel>();

                    List<Policy> policies;
                    var clientId = User.FindFirstValue("client_id");
                    policies = await session.Query<Policy>().Where(t => t.ClientId.Equals(clientId)).Take(1024).ToListAsync();

                    foreach (var policy in policies)
                    {
                        model.Add(new PolicyModel
                        {
                            AuthenticationSchemes = policy.AuthenticationSchemes,
                            Description = policy.Description,
                            Id = policy.Id,
                            Name = policy.Name,
                            Requirements = policy.Requirements
                        });
                    }
                    return Ok(model);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get policies");
                throw;
            }
        }

        /// <summary>
        /// Get a policy by id
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
                using (var session = _documentStore.OpenAsyncSession())
                {
                    var clientId = User.FindFirstValue("client_id");
                    var policy = await session.Query<Policy>().FirstOrDefaultAsync(t => t.ClientId.Equals(clientId) && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (policy == null)
                        throw new KeyNotFoundException($"Policy {name} was not found");

                    var model = new PolicyModel
                    {
                        AuthenticationSchemes = policy.AuthenticationSchemes,
                        Description = policy.Description,
                        Id = policy.Id,
                        Name = policy.Name,
                        Requirements = policy.Requirements
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
