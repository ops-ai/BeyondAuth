﻿using BeyondAuth.Acl;
using Identity.Core;
using IdentityManager.Models;
using IdentityServer4.Contrib.RavenDB.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading;
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
        private readonly IDocumentStore _store;
        private readonly IAuthorizationService _authorizationService;
        private readonly IOptions<IdentityStoreOptions> _identityStoreOptions;

        public UsersController(UserManager<ApplicationUser> userManager, ILogger<UsersController> logger, IDocumentStore store, IAuthorizationService authorizationService, IOptions<IdentityStoreOptions> identityStoreOptions)
        {
            _userManager = userManager;
            _logger = logger;
            _store = store;
            _authorizationService = authorizationService;
            _identityStoreOptions = identityStoreOptions;
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
        /// <param name="skip">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="take">Result range to return. Format: 0-19 (result index from - result index to)</param>
        /// <param name="ct"></param>
        /// <response code="206">List of users matching criteria</response>
        /// <response code="400">Validation failed.</response>
        /// <response code="500">Server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IList<UserModel>), (int)HttpStatusCode.PartialContent)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string dataSourceId, [FromQuery] string firstName, [FromQuery] string lastName, [FromQuery] string displayName, [FromQuery] string organization, [FromQuery] string email, [FromQuery] bool includeDisabled = false, [FromQuery] bool lockedOnly = false, [FromQuery] string sort = "+email", [FromQuery] int skip = 0, [FromQuery] int take = 0, CancellationToken ct = default)
        {
            try
            {
                //using (var session = _store.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                //{
                //    var dataSource = await session.Include<IdPSettings>(t => t.NearestSecurityHolderId).LoadAsync<TenantSetting>($"TenantSettings/{dataSourceId}", ct);
                //    dataSource.AclHolder = dataSource.NearestSecurityHolderId != null ? await session.LoadAsync<ISecurableEntity>(dataSource.NearestSecurityHolderId, ct) : null;
                //    if (await _authorizationService.AuthorizeAsync(User, dataSource, AclPermissions.List).ContinueWith(s => s.Result.Succeeded))
                //        throw new UnauthorizedAccessException();
                //}

                //TODO: Add direct querying + filtering + permssion filtering

                var query = ((IRavenQueryable<ApplicationUser>)_userManager.Users).Statistics(out var stats);
                if (firstName != null)
                    query = query.Where(t => t.FirstName.StartsWith(firstName));
                if (lastName != null)
                    query = query.Where(t => t.LastName.StartsWith(lastName));
                if (displayName != null)
                    query = query.Where(t => t.DisplayName.StartsWith(displayName));
                if (organization != null)
                    query = query.Where(t => t.Organization.StartsWith(organization));
                if (email != null)
                    query = query.Where(t => t.Email.StartsWith(email));
                if (!includeDisabled)
                    query = query.Where(t => !t.Disabled);
                if (lockedOnly)
                    query = query.Where(t => t.LockoutEnabled && t.LockoutEnd != null && t.LockoutEnd > DateTime.UtcNow);

                query = sort switch
                {
                    "+email" => query.OrderBy(t => t.Email),
                    "-email" => query.OrderByDescending(t => t.Email),
                    "+firstName" => query.OrderBy(t => t.FirstName),
                    "-firstName" => query.OrderByDescending(t => t.FirstName),
                    "+lastName" => query.OrderBy(t => t.LastName),
                    "-lastName" => query.OrderByDescending(t => t.LastName),
                    "+organization" => query.OrderBy(t => t.Organization),
                    "-organization" => query.OrderByDescending(t => t.Organization),
                    _ => query.OrderBy(t => t.Email),
                };

                var users = await query.Skip(skip).Take(take).Select(t => new UserModel
                {
                    AccountExpiration = t.AccountExpiration,
                    ChangePasswordAllowed = t.ChangePasswordAllowed,
                    ChangePasswordOnNextLogin = t.ChangePasswordOnNextLogin,
                    Claims = t.Claims.ToDictionary(s => s.ClaimType, s => s.ClaimValue),
                    Disabled = t.Disabled,
                    DisplayName = t.DisplayName,
                    Email = t.Email,
                    FirstName = t.FirstName,
                    Id = t.Id.Substring("ApplicationUsers/".Length),
                    LastLoggedIn = t.LastLoggedIn,
                    LastName = t.LastName,
                    Locked = t.LockoutEnabled && t.LockoutEnd != null && t.LockoutEnd > DateTime.UtcNow,
                    LockoutEnabled = t.LockoutEnabled,
                    LockoutEnd = t.LockoutEnd != null ? t.LockoutEnd.Value.DateTime : null,
                    Organization = t.Organization,
                    //PasswordExpiry = ,
                    PasswordPolicy = t.PasswordPolicy,
                    PasswordResetAllowed = t.PasswordResetAllowed,
                    PhoneNumbers = t.PhoneNumbers,
                    ZoneInfo = t.ZoneInfo,
                    AccessFailedCount = t.AccessFailedCount,
                    CreatedOnUtc = t.CreatedOnUtc,
                    DefaultApp = t.DefaultApp,
                    EmailConfirmed = t.EmailConfirmed,
                    Locale = t.Locale,
                    TwoFactorEnabled = t.TwoFactorEnabled,
                    UpdatedAt = t.UpdatedAt
                }).ToListAsync(ct);

                Response.Headers.Add("X-Total-Count", stats.TotalResults.ToString());

                return Ok(users);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");

                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(0, ex, "Error during GET user");

                throw;
            }
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
        /// <param name="dataSourceId"></param>
        /// <param name="userId">User Unique ID or email</param>
        /// <param name="ct"></param>
        /// <response code="200">User details</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Server error</response>
        [HttpGet("{userId}")]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetUser([FromRoute] string dataSourceId, [FromRoute] string userId, CancellationToken ct = default)
        {
            try
            {
                //using (var session = _store.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                //{
                //    var dataSource = await session.Include<TenantSetting>(t => t.NearestSecurityHolderId).LoadAsync<TenantSetting>($"TenantSettings/{dataSourceId}", ct);
                //    dataSource.AclHolder = dataSource.NearestSecurityHolderId != null ? await session.LoadAsync<ISecurableEntity>(dataSource.NearestSecurityHolderId, ct) : null;
                //    if (await _authorizationService.AuthorizeAsync(User, dataSource, AclPermissions.List).ContinueWith(s => s.Result.Succeeded))
                //        throw new UnauthorizedAccessException();
                //}

                var user = await RetrieveUser(dataSourceId, userId, ct);
                if (user == null)
                    throw new KeyNotFoundException(userId);

                return Ok(user);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found");

                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access");

                return Forbid();
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
        /// <param name="ct"></param>
        /// <response code="200">User created</response>
        /// <response code="400">Validation failed</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(UserModel), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.Forbidden)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Post([FromRoute] string dataSourceId, [FromBody] UserCreateModel userInfo, CancellationToken ct = default)
        {
            try
            {
                //using (var session = _store.OpenAsyncSession())
                //{
                //    var dataSource = await session.Include<TenantSetting>(t => t.NearestSecurityHolderId).LoadAsync<TenantSetting>($"TenantSettings/{dataSourceId}", ct);
                //    dataSource.AclHolder = dataSource.NearestSecurityHolderId != null ? await session.LoadAsync<ISecurableEntity>(dataSource.NearestSecurityHolderId, ct) : null;
                //    if (await _authorizationService.AuthorizeAsync(User, dataSource, AclPermissions.List).ContinueWith(s => s.Result.Succeeded, ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default))
                //        throw new UnauthorizedAccessException();
                //}

                if (!ModelState.IsValid)
                    return ValidationProblem(ModelState);
                
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
                    ZoneInfo = userInfo.ZoneInfo,
                    LockoutEnabled = userInfo.LockoutEnabled
                };

                foreach (var claim in userInfo.Claims.Select(t => new Raven.Identity.IdentityUserClaim { ClaimType = t.Key, ClaimValue = t.Value }))
                    newUser.Claims.Add(claim);

                var result = await _userManager.CreateAsync(newUser, userInfo.Password);

                if (result.Succeeded)
                    return Ok(ToUserModel(newUser));
                else
                {
                    foreach (var error in result.Errors.Where(t => t.Code.Contains("Password", StringComparison.OrdinalIgnoreCase)))
                        ModelState.AddModelError(nameof(userInfo.Password), error.Description);
                    foreach (var error in result.Errors.Where(t => !t.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) && t.Code != "DuplicateUserName"))
                        ModelState.AddModelError(error.Code ?? "Password", error.Description);
                    
                    return ValidationProblem(ModelState);
                }
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
        /// <param name="ct"></param>
        /// <response code="204">User was updated</response>
        /// <response code="400">Validation failed. Returns a list of fields and errors for each field</response>
        /// <response code="404">User was not found</response>
        /// <response code="500">Oops! Can't update this user right now</response>
        [HttpPut("{userId}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(IDictionary<string, string>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> Put([FromRoute] string dataSourceId, [FromRoute] string userId, [FromBody] UserUpdateModel userInfo, CancellationToken ct = default)
        {
            try
            {
                //using (var session = _store.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                //{
                //    var dataSource = await session.Include<TenantSetting>(t => t.NearestSecurityHolderId).LoadAsync<TenantSetting>($"TenantSettings/{dataSourceId}", ct);
                //    dataSource.AclHolder = dataSource.NearestSecurityHolderId != null ? await session.LoadAsync<ISecurableEntity>(dataSource.NearestSecurityHolderId, ct) : null;
                //    if (await _authorizationService.AuthorizeAsync(User, dataSource, AclPermissions.List).ContinueWith(s => s.Result.Succeeded))
                //        throw new UnauthorizedAccessException();
                //}

                var user = await _userManager.FindByIdAsync(userId);
                if (userInfo.Password != null)
                {
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, code, userInfo.Password);
                }

                if (!user.Email.Equals(userInfo.Email, StringComparison.OrdinalIgnoreCase))
                {
                    await _userManager.SetEmailAsync(user, userInfo.Email);
                    await _userManager.SetUserNameAsync(user, userInfo.Email);
                }

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
                user.LockoutEnabled = userInfo.LockoutEnabled;

                if (userInfo.ChangePasswordOnNextLogin.HasValue)
                    user.ChangePasswordOnNextLogin = userInfo.ChangePasswordOnNextLogin.Value;

                if (user.LockoutEnd > DateTime.UtcNow && userInfo.Locked == false)
                    user.LockoutEnd = null;

                await _userManager.UpdateAsync(user);

                return NoContent();
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

        private async Task<UserModel> RetrieveUser([FromRoute] string dataSourceId, [FromRoute] string userId, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                user = await _userManager.FindByIdAsync($"ApplicationUsers/{userId}");
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
            Id = user.Id.Split('/').Last(),
            LastLoggedIn = user.LastLoggedIn,
            LastName = user.LastName,
            Locked = user.LockoutEnabled && user.LockoutEnd > DateTime.UtcNow,
            LockoutEnd = user.LockoutEnd?.DateTime,
            Organization = user.Organization,
            PasswordPolicy = user.PasswordPolicy,
            PasswordResetAllowed = user.PasswordResetAllowed,
            PhoneNumbers = user.PhoneNumbers,
            ZoneInfo = user.ZoneInfo,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            CreatedOnUtc = user.CreatedOnUtc,
            DefaultApp = user.DefaultApp,
            EmailConfirmed = user.EmailConfirmed,
            Locale = user.Locale,
            TwoFactorEnabled = user.TwoFactorEnabled,
            UpdatedAt = user.UpdatedAt
        };

        /// <summary>
        /// Update one or more properties on a user
        /// </summary>
        /// <param name="dataSourceId"></param>
        /// <param name="userId"></param>
        /// <param name="patch"></param>
        /// <param name="ct"></param>
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
        public async Task<IActionResult> Patch([FromRoute] string dataSourceId, [FromRoute] string userId, [FromBody] JsonPatchDocument<UserUpdateModel> patch, CancellationToken ct = default)
        {
            try
            {
                //using (var session = _store.OpenAsyncSession(_identityStoreOptions.Value.DatabaseName))
                //{
                //    var dataSource = await session.Include<TenantSetting>(t => t.NearestSecurityHolderId).LoadAsync<TenantSetting>($"TenantSettings/{dataSourceId}", ct);
                //    dataSource.AclHolder = dataSource.NearestSecurityHolderId != null ? await session.LoadAsync<ISecurableEntity>(dataSource.NearestSecurityHolderId, ct) : null;
                //    if (await _authorizationService.AuthorizeAsync(User, dataSource, AclPermissions.List).ContinueWith(s => s.Result.Succeeded))
                //        throw new UnauthorizedAccessException();
                //}

                var originalUser = await RetrieveUser(dataSourceId, userId, ct);
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
                    ZoneInfo = originalUser.ZoneInfo,
                    LockoutEnabled = originalUser.LockoutEnabled,
                    Locked = originalUser.LockoutEnd > DateTime.UtcNow
                };

                patch.ApplyTo(updatedUser);
                return await Put(dataSourceId, userId, updatedUser, ct);
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
