using Identity.Core.Exceptions;
using IdentityModel;
using IdentityServer.LdapExtension;
using IdentityServer.LdapExtension.UserModel;
using IdentityServer.LdapExtension.UserStore;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Identity.Core
{
    public class RavenDBUserStore<TUser> : ILdapUserStore 
        where TUser : IAppUser, new()
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<RavenDBUserStore<TUser>> _logger;
        private readonly ILdapService<TUser> _authenticationService;

        /// <summary>
        /// Initializes a new instance of the <see cref="RavenDBUserStore{TUser}"/> class.
        /// </summary>
        /// <param name="authenticationService">The Ldap authentication service.</param>
        public RavenDBUserStore(ILogger<RavenDBUserStore<TUser>> logger, IDocumentStore documentStore, ILdapService<TUser> authenticationService)
        {
            _documentStore = documentStore;
            _logger = logger;
            _authenticationService = authenticationService;
        }

        public IAppUser AutoProvisionUser(string provider, string userId, List<Claim> claims)
        {
            using (var session = _documentStore.OpenSession())
            {
                var filtered = new List<Claim>();

                foreach (var claim in claims)
                {
                    // if the external system sends a display name - translate that to the standard OIDC name claim
                    if (claim.Type == ClaimTypes.Name)
                    {
                        filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                    }
                    // if the JWT handler has an outbound mapping to an OIDC claim use that
                    else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
                    {
                        filtered.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
                    }
                    // copy the claim as-is
                    else
                    {
                        filtered.Add(claim);
                    }
                }

                // if no display name was provided, try to construct by first and/or last name
                if (!filtered.Any(x => x.Type == JwtClaimTypes.Name))
                {
                    var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
                    var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
                    if (first != null && last != null)
                    {
                        filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                    }
                    else if (first != null)
                    {
                        filtered.Add(new Claim(JwtClaimTypes.Name, first));
                    }
                    else if (last != null)
                    {
                        filtered.Add(new Claim(JwtClaimTypes.Name, last));
                    }
                }

                // create a new unique subject id
                var sub = CryptoRandom.CreateUniqueId();

                // check if a display name is available, otherwise fallback to subject id
                var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? sub;

                // create new user
                var user = new TUser
                {
                    SubjectId = sub,
                    Username = name,
                    ProviderName = provider,
                    ProviderSubjectId = userId,
                    Claims = filtered
                };

                StoreOrUpdateUser(user);

                return user;
            }
        }

        /// <summary>
        /// Finds the user by external provider.
        /// </summary>
        /// <param name="provider">The provider.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns></returns>
        public IAppUser FindByExternalProvider(string provider, string userId)
        {
            using (var session = _documentStore.OpenSession())
            {
                return session.Query<TUser>().FirstOrDefault(t => t.ProviderName == provider && t.ProviderSubjectId == userId);
            }
        }

        /// <summary>
        /// Finds the user by subject identifier, but does not add the user to the cache
        /// since he's not logged in, in the current context.
        /// </summary>
        /// <param name="subjectId">The subject identifier.</param>
        /// <returns>The application user.</returns>
        public IAppUser FindBySubjectId(string subjectId)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Load<TUser>(subjectId);
                if (user != null)
                    return user;
            }

            // Search in the LDAP
            if (subjectId.Contains("ldap_"))
            {
                var user = _authenticationService.FindUser(subjectId.Replace("ldap_", "")); // As of now, subjectId is the same as the username

                if (user != null)
                {
                    StoreOrUpdateUser(user);
                    return user;
                }
            }

            // Not found at all
            return null;
        }

        /// <summary>
        /// Finds by username.
        /// </summary>
        /// <param name="username">The username that we are want to find.</param>
        /// <returns>
        /// Returns the application user that match the requested username.
        /// </returns>
        public IAppUser FindByUsername(string username)
        {
            using (var session = _documentStore.OpenSession())
            {
                var user = session.Query<TUser>().FirstOrDefault(t => t.Username == username);
                if (user != null)
                    return user;

                // If nothing found in external, than we look in our current LDAP system. (We want to get always the latest details when we are on the LDAP).
                var ldapUser = _authenticationService.FindUser(username);
                if (ldapUser != null)
                {
                    StoreOrUpdateUser(ldapUser);
                }

                // Not found at all
                return ldapUser;
            }
        }

        /// <summary>
        /// Validates the credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>
        /// Returns the application user that match that account if the
        /// authentication is successful.
        /// </returns>
        public IAppUser ValidateCredentials(string username, string password)
        {
            try
            {
                var user = _authenticationService.Login(username, password);
                if (user != null)
                {
                    StoreOrUpdateUser(user);

                    return user;
                }
            }
            catch (LoginFailedException ex)
            {
                _logger.LogWarning(ex, "Login failed");

                return default(TUser);
            }

            return default(TUser);
        }

        /// <summary>
        /// Validates the credentials.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain friendly name.</param>
        /// <returns>
        /// Returns the application user that match that account if the
        /// authentication is successful.
        /// </returns>
        public IAppUser ValidateCredentials(string username, string password, string domain)
        {
            try
            {
                var user = _authenticationService.Login(username, password, domain);
                if (user != null)
                {
                    StoreOrUpdateUser(user);

                    return user;
                }
            }
            catch (LoginFailedException ex)
            {
                _logger.LogWarning(ex, "Login failed");

                return default(TUser);
            }

            return default(TUser);
        }

        private void StoreOrUpdateUser(IAppUser user)
        {
            using (var session = _documentStore.OpenSession())
            {
                session.Store(user, user.SubjectId);
                session.SaveChanges();
            }
        }
    }
}
