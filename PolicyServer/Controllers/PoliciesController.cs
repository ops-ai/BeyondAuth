using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PolicyServer.Models;

namespace PolicyServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PoliciesController : ControllerBase
    {
        private readonly ILogger<PoliciesController> _logger;

        public PoliciesController(ILogger<PoliciesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IEnumerable<PolicyModel> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new PolicyModel
            {

            })
            .ToArray();
        }
    }
}
