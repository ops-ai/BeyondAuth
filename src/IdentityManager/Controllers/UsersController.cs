﻿using Identity.Core;
using IdentityManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSwag.Annotations;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace IdentityManager.Controllers
{
    [Route("{dataSourceId}/users")]
    [ApiController]
    [OpenApiTag("Users", AddToDocument = true, DocumentationDescription = "Manage users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _logger = logger;
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
        [ProducesResponseType(typeof(IList<UserModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get([FromQuery] string firstName, [FromQuery] string lastName, [FromQuery] string displayName, [FromQuery] string organization, [FromQuery] string email, [FromQuery] bool includeDisabled = false, [FromQuery] bool lockedOnly = false, [FromQuery] string sort = "+email", [FromQuery] string range = "0-19")
        {
            return Ok(await _userManager.Users.ToListAsync());
        }

        /// <summary>
        /// Check if a user exists
        /// </summary>
        /// <param name="userId">User Unique ID or email</param>
        /// <response code="200">User exists</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Server error</response>
        [HttpHead("{userId}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public IActionResult Head(string userId) => Ok(); //Placeholder for swagger to work

        /// <summary>
        /// Get a user
        /// </summary>
        /// <param name="userId">User Unique ID or email</param>
        /// <response code="200">User details</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                return Ok(await RetrieveUser(userId));
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
        /// <param name="userInfo">The user's information</param>
        /// <response code="200">User created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromBody] UserCreateModel userInfo)
        {
            try
            {
                var newUser = new ApplicationUser
                {
                    UserName = userInfo.Email,
                    ChangePasswordOnNextLogin = userInfo.ChangePasswordOnNextLogin ?? false,
                    ChangePasswordAllowed = userInfo.ChangePasswordAllowed,
                    CreatedOnUtc = DateTime.UtcNow,
                    Disabled = userInfo.Disabled,
                    DisplayName = userInfo.DisplayName,
                    Email = userInfo.Email,
                    FirstName = userInfo.FirstName,
                    LastName = userInfo.LastName,
                    Organization = userInfo.Organization,
                    PasswordResetAllowed = userInfo.PasswordResetAllowed,
                    PasswordPolicy = userInfo.PasswordPolicy,
                    AccountExpiration = userInfo.AccountExpiration,
                    ZoneInfo = userInfo.ZoneInfo
                };

                foreach (var claim in userInfo.Claims.Select(t => new Raven.Identity.IdentityUserClaim { ClaimType = t.Key, ClaimValue = t.Value }))
                    newUser.Claims.Add(claim);

                var result = await _userManager.CreateAsync(newUser, userInfo.Password);

                if (result.Succeeded)
                    return Ok(ToUserModel(newUser));
                else
                    return BadRequest(result.Errors.ToDictionary(t => t.Code, t => t.Description));
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
        /// <param name="userId"></param>
        /// <param name="userInfo"></param>
        /// <response code="204">User was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Oops! Can't update this user right now</response>
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Put([FromRoute] string userId, [FromBody] UserUpdateModel userInfo)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (userInfo.Password != null)
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, code, userInfo.Password);
                }

                if (!user.Email.Equals(userInfo.Email, StringComparison.OrdinalIgnoreCase))
                    await _userManager.SetEmailAsync(user, userInfo.Email);

                user.FirstName = userInfo.FirstName;
                user.LastName = userInfo.LastName;
                user.DisplayName = userInfo.DisplayName;
                user.Organization = userInfo.Organization;
                user.PasswordResetAllowed = userInfo.PasswordResetAllowed;
                user.ChangePasswordAllowed = userInfo.ChangePasswordAllowed;
                user.PasswordPolicy = userInfo.PasswordPolicy;
                user.AccountExpiration = userInfo.AccountExpiration;
                user.Disabled = userInfo.Disabled;
                user.ZoneInfo = userInfo.ZoneInfo;

                if (userInfo.ChangePasswordOnNextLogin.HasValue)
                    user.ChangePasswordOnNextLogin = userInfo.ChangePasswordOnNextLogin.Value;

                await _userManager.UpdateAsync(user);

                return NoContent();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, $"Validation error: {ex.Message} while attempting to update user {userId}.");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, $"User not found when attempting to update user {userId}.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while attempting to update user {userId}.");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }

        private async Task<UserModel> RetrieveUser([FromRoute] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            return ToUserModel(user);
        }

        private static UserModel ToUserModel(ApplicationUser user) => new()
        {
            AccountExpiration = user.AccountExpiration,
            ChangePasswordAllowed = user.ChangePasswordAllowed,
            ChangePasswordOnNextLogin = user.ChangePasswordOnNextLogin,
            Claims = user.Claims.ToDictionary(t => t.ClaimType, t => t.ClaimValue),
            Disabled = user.Disabled,
            DisplayName = user.DisplayName,
            Email = user.Email,
            FirstName = user.FirstName,
            Id = user.Id,
            LastLoggedIn = user.LastLoggedIn,
            LastName = user.LastName,
            Locked = user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow,
            LockoutEnd = user.LockoutEnd?.DateTime,
            Organization = user.Organization,
            PasswordPolicy = user.PasswordPolicy,
            PasswordResetAllowed = user.PasswordResetAllowed,
            PhoneNumbers = user.PhoneNumbers,
            ZoneInfo = user.ZoneInfo
        };

        /// <summary>
        /// Update one or more properties on a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="patch"></param>
        /// <remarks>This is the preferred way to modify a user</remarks>
        /// <response code="204">User was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Oops! Can't update this user right now</response>
        [HttpPatch("{userId}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Patch([FromRoute] string userId, [FromBody] JsonPatchDocument<UserUpdateModel> patch)
        {
            try
            {
                var originalUser = await RetrieveUser(userId);
                _logger.LogInformation($"Get the user object for Patching user:{userId}");

                if (originalUser == null)
                    throw new KeyNotFoundException($"User {userId} was not found");

                var updatedUser = new UserUpdateModel
                {
                    ChangePasswordAllowed = originalUser.ChangePasswordAllowed,
                    ChangePasswordOnNextLogin = originalUser.ChangePasswordOnNextLogin,
                    Claims = originalUser.Claims,
                    Disabled = originalUser.Disabled,
                    Email = originalUser.Email,
                    FirstName = originalUser.FirstName,
                    LastName = originalUser.LastName,
                    Organization = originalUser.Organization,
                    PasswordPolicy = originalUser.PasswordPolicy,
                    PasswordResetAllowed = originalUser.PasswordResetAllowed,
                    AccountExpiration = originalUser.AccountExpiration,
                    ZoneInfo = originalUser.ZoneInfo
                };

                patch.ApplyTo(updatedUser);
                return await Put(userId, updatedUser);
            }
            catch (JsonPatchException ex)
            {
                _logger.LogError(ex, $"Invalid JsonPatch Operation:{ex.FailedOperation.OperationType} while attempting to update user {userId}.");
                if (ex.FailedOperation.OperationType == OperationType.Test)
                    return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
                else
                    return BadRequest(new Dictionary<string, string> { { "reason", ex.FailedOperation.op } });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, $"User not found when attempting to update user {userId}.");
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while attempting to update user {userId}.");
                return BadRequest(new Dictionary<string, string> { { "reason", ex.Message } });
            }
        }
    }
}
