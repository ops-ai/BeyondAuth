using IdentityManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;

namespace PolicyServer.Controllers
{
    [Route("{dataSourceId}/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IDocumentStore documentStore, ILogger<UsersController> logger)
        {
            _documentStore = documentStore;
            _logger = logger;
        }

        /// <summary>
        /// Find users
        /// </summary>
        /// <param name="dataSourceId"></param>
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
        [Authorize]
        [ProducesResponseType(typeof(IList<UserModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string dataSourceId, [FromQuery] string firstName, [FromQuery] string lastName, [FromQuery] string displayName, [FromQuery] string organization, [FromQuery] string email, [FromQuery] bool includeDisabled = false, [FromQuery] bool lockedOnly = false, [FromQuery] string sort = "+email", [FromQuery] string range = "0-19")
        {
            return Ok();
        }

        /// <summary>
        /// Check if a user exists
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userId">User Unique ID or email</param>
        /// <response code="200">User exists</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Server error</response>
        [HttpHead("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public IActionResult Head([FromRoute] string dataSourceId, string userId)
        {
            //Placeholder for swagger to work
            return Ok();
        }

        /// <summary>
        /// Get a user
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userId">User Unique ID or email</param>
        /// <response code="200">User details</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetUser([FromRoute] string dataSourceId, string userId)
        {
            try
            {
                throw new NotImplementedException();
                //return Ok(await RetrieveUser(userId));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error during GET user");

                throw;
            }
        }

        /// <summary>
        /// Create user
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userInfo">The user's information</param>
        /// <response code="200">User created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromRoute] string dataSourceId, [FromBody] UserCreateModel userInfo)
        {
            try
            {
                throw new NotImplementedException();

            }
            catch (System.Data.DuplicateNameException ex)
            {
                _logger.LogWarning(ex, $"User {userInfo.Email} already exists");
                return BadRequest(new Dictionary<string, string> { { "reason", "User already exists" } });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(0, ex, "Validation exception");

                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, $"Error creating user");
                throw;
            }
        }

        /// <summary>
        /// Replace all properties on a user with this data
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userId"></param>
        /// <param name="userInfo"></param>
        /// <response code="204">User was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Oops! Can't update this user right now</response>
        [HttpPut("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Put([FromRoute] string dataSourceId, [FromRoute] string userId, [FromBody] UserUpdateModel userInfo)
        {
            throw new NotImplementedException();
        }

        private async Task<UserModel> FindUser([FromRoute] string dataSourceId, [FromRoute] string userId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update one or more properties on a user
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userId"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify a user</remarks>
        /// <response code="204">User was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Oops! Can't update this user right now</response>
        [HttpPatch("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(void), 204)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 400)]
        [ProducesResponseType(typeof(void), 500)]
        public async Task<IActionResult> Patch([FromRoute] string dataSourceId, [FromRoute] string userId, [FromBody] JsonPatchDocument<UserUpdateModel> patch)
        {
            try
            {
                var originalUser = await FindUser(dataSourceId, userId);
                _logger.LogInformation($"Get the user object for Patching user:{userId}");

                var updatedUser = new UserUpdateModel
                {
                    ChangePasswordAllowed = originalUser.ChangePasswordAllowed,
                    ChangePasswordOnNextLogin = originalUser.ChangePasswordOnNextLogin,
                    Claims = originalUser.Claims,
                    Disabled = originalUser.Disabled,
                    Email = originalUser.Email,
                    FirstName = originalUser.FirstName,
                    LastName = originalUser.LastName,
                    Locked = originalUser.Locked,
                    Organization = originalUser.Organization,
                    PasswordPolicy = originalUser.PasswordPolicy,
                    PasswordResetAllowed = originalUser.PasswordResetAllowed,
                    AccountExpiration = originalUser.AccountExpiration,
                    ZoneInfo = originalUser.ZoneInfo
                };

                patch.ApplyTo(updatedUser);
                return await Put(dataSourceId, userId, updatedUser);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update user {userId}.");
                if (ex.FailedOperation.OperationType == OperationType.Test)
                    return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
                else
                    return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while attempting to update user {userId}.");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }
    }
}
