using IdentityModel.Client;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Text;
using BeyondAuth.UserManagement.Helpers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace BeyondAuth.UserManagement
{
    public class BeyondAuthUserStore<TUser> : IUserStore<TUser>, IUserEmailStore<TUser>, IUserPhoneNumberStore<TUser>, IUserSecurityStampStore<TUser>, IPasswordHasher<TUser>, IUserPasswordStore<TUser>
        where TUser : IdPUser
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IOptions<IdPSettings> _idPsettings;
        private JsonPatchDocument<TUser> _unsavedChanges = new JsonPatchDocument<TUser>();

        public BeyondAuthUserStore(IHttpClientFactory clientFactory, IOptions<IdPSettings> authenticationSettings)
        {
            _clientFactory = clientFactory;
            _idPsettings = authenticationSettings;
        }

        #region IUserStore

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            using (var httpClient = _clientFactory.CreateClient("idP"))
            {
                var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = _idPsettings.Value.Authority + "/connect/token",

                    ClientId = _idPsettings.Value.IdPClientId,
                    ClientSecret = _idPsettings.Value.IdPClientSecret,
                    Scope = "idp"
                }, cancellationToken: cancellationToken);

                httpClient.SetBearerToken(tokenResponse.AccessToken);

                var createUserResponse = await httpClient.PostAsJsonAsync($"{_idPsettings.Value.IdPBaseUrl}/users", user, cancellationToken);
                if (createUserResponse.IsSuccessStatusCode)
                {
                    _unsavedChanges = new JsonPatchDocument<TUser>();
                    return IdentityResult.Success;
                }
                else
                {
                    var problems = await createUserResponse.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken: cancellationToken);
                    if (problems != null)
                        return IdentityResult.Failed(problems.Errors.Select(t => new IdentityError { Code = t.Key, Description = t.Value.FirstOrDefault() }).ToArray());
                    else
                        return IdentityResult.Failed(new IdentityError { Code = "No Response", Description = "No response from identity api" });
                }
            }
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken) => throw new NotImplementedException();

        public void Dispose() { }

        public Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => FindByEmailAsync(userId, cancellationToken);

        public Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => FindByEmailAsync(normalizedUserName, cancellationToken);

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToLower());

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id);

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (!_unsavedChanges.Operations.Any())
                return IdentityResult.Success;

            using (var httpClient = _clientFactory.CreateClient("idP"))
            {
                var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = _idPsettings.Value.Authority + "/connect/token",

                    ClientId = _idPsettings.Value.IdPClientId,
                    ClientSecret = _idPsettings.Value.IdPClientSecret,
                    Scope = "idp"
                }, cancellationToken: cancellationToken);

                if (tokenResponse.IsError)
                    Console.WriteLine(tokenResponse.Error);

                httpClient.SetBearerToken(tokenResponse.AccessToken);

                var content = new StringContent(JsonConvert.SerializeObject(_unsavedChanges), Encoding.UTF8, "application/json-patch+json");
                var response = await httpClient.PatchAsync($"{_idPsettings.Value.IdPBaseUrl}/users/{user.Id.Split('/').Last()}", content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _unsavedChanges = new JsonPatchDocument<TUser>();
                    return IdentityResult.Success;
                }
                else
                {
                    var problems = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken: cancellationToken);
                    if (problems != null)
                        return IdentityResult.Failed(problems.Errors.Select(t => new IdentityError { Code = t.Key, Description = t.Value.FirstOrDefault() }).ToArray());
                    else
                        return IdentityResult.Failed(new IdentityError { Code = "No Response", Description = "No response from identity api" });
                }
            }
        }

        #endregion


        #region IUserEmailStore

        public async Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            using (var httpClient = _clientFactory.CreateClient("idP"))
            {
                var tokenResponse = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
                {
                    Address = _idPsettings.Value.Authority + "/connect/token",

                    ClientId = _idPsettings.Value.IdPClientId,
                    ClientSecret = _idPsettings.Value.IdPClientSecret,
                    Scope = "idp"
                }, cancellationToken: cancellationToken);

                httpClient.SetBearerToken(tokenResponse.AccessToken);

                return await httpClient.GetFromJsonAsync<TUser>($"{_idPsettings.Value.IdPBaseUrl}/users/{normalizedEmail}", cancellationToken);
            }
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email);

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.EmailConfirmed);

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.Email.ToLower());

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken)
        {
            if (user.Email != email)
            {
                user.Email = email;
                _unsavedChanges = _unsavedChanges.Replace(t => t.Email, email);
            }
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user.EmailConfirmed != confirmed)
            {
                user.EmailConfirmed = confirmed;
                _unsavedChanges = _unsavedChanges.Replace(t => t.EmailConfirmed, confirmed);
            }
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken) => Task.CompletedTask;

        #endregion


        #region IUserPhoneNumberStore

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            if (user.PhoneNumber != phoneNumber)
            {
                user.PhoneNumber = phoneNumber;
                _unsavedChanges = _unsavedChanges.Replace(t => t.PhoneNumber, phoneNumber);
            }
            return Task.CompletedTask;
        }

        public Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumber);

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.PhoneNumberConfirmed);

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            if (user.PhoneNumberConfirmed != confirmed)
            {
                user.PhoneNumberConfirmed = confirmed;
                _unsavedChanges = _unsavedChanges.Replace(t => t.PhoneNumberConfirmed, confirmed);
            }
            return Task.CompletedTask;
        }

        #endregion


        #region IUserSecurityStampStore

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken) => Task.FromResult(user.SecurityStamp);

        #endregion


        #region IPasswordHasher

        public string HashPassword(TUser user, string password) => password;

        public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword) => throw new NotImplementedException();

        #endregion


        #region IUserPasswordStore

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            _unsavedChanges = _unsavedChanges.Replace(t => t.Password, passwordHash);
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken) => throw new NotImplementedException();

        #endregion
    }
}