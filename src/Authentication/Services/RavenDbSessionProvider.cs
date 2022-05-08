using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Authentication.Services
{
    public class RavenDbSessionProvider : IUserSession
    {
        public Task AddClientIdAsync(string clientId) => throw new NotImplementedException();
        public Task<string> CreateSessionIdAsync(ClaimsPrincipal principal, AuthenticationProperties properties) => throw new NotImplementedException();
        public Task EnsureSessionIdCookieAsync() => throw new NotImplementedException();
        public Task<IEnumerable<string>> GetClientListAsync() => throw new NotImplementedException();
        public Task<string> GetSessionIdAsync() => throw new NotImplementedException();
        public Task<ClaimsPrincipal> GetUserAsync() => throw new NotImplementedException();
        public Task RemoveSessionIdCookieAsync() => throw new NotImplementedException();
    }
}
