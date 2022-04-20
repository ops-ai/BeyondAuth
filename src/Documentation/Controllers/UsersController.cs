using Microsoft.AspNetCore.Mvc;

namespace PolicyServer.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public TestController()
        {

        }

        /// <summary>
        /// Find users
        /// </summary>
        /// <param name="firstName">First Name</param>
        /// <param name="lastName">Last Name</param>
        /// <param name="displayName">Display Name</param>
        /// <param name="organization">Organization</param>
        /// <param name="email">Email</param>
        /// <param name="includeDisabled">Include disabled accounts</param>
        /// <param name="lockedOnly">Show only locked accounts</param>
        /// <param name="sort">Field and direction to sort by. Format: ([+/-]FieldName) By default: +email (sort by email ascending)</param>
        /// <param name="range">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <response code="206">List of users matching criteria</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        public async Task<IActionResult> Get() => throw new NotImplementedException();
    }
}
