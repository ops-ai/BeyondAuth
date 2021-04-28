using BeyondAuth.Web.Models.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BeyondAuth.Web.Services
{
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly ILogger _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <inheritdoc />
        public ClaimsTransformer(
            ILoggerFactory loggerFactory,
            SignInManager<ApplicationUser> signInManager)
        {
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            _signInManager = signInManager;
        }

        /// <summary>
        /// Transform and enrich current user's principal
        /// </summary>
        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            try
            {

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tranforming principal");
            }

            return principal;
        }
    }
}
