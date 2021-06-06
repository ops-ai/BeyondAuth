// License: MIT
// Source (with minor modifications): https://github.com/andrewlock/NetEscapades.AspNetCore.Identity.Validators

using Microsoft.AspNetCore.Identity;
using Raven.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Common
{
    public class InvalidPhrasePasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : IdentityUser
    {
        private readonly HashSet<string> _invalidPhrases;
        public InvalidPhrasePasswordValidator(IEnumerable<string> invalidPhrases)
        {
            if (invalidPhrases == null) { throw new ArgumentNullException(nameof(invalidPhrases)); }
            _invalidPhrases = new HashSet<string>(invalidPhrases, StringComparer.OrdinalIgnoreCase);
        }

        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (password == null) { throw new ArgumentNullException(nameof(password)); }
            if (manager == null) { throw new ArgumentNullException(nameof(manager)); }

            var result = _invalidPhrases.Contains(password) ? 
                IdentityResult.Failed(new IdentityError { Code = "InvalidPhrase", Description = $"You cannot use '{password}' as your password" }) : 
                IdentityResult.Success;

            return Task.FromResult(result);
        }
    }
}
