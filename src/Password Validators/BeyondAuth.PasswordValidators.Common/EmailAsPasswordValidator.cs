using Microsoft.AspNetCore.Identity;
using Raven.Identity;
using System;
using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Common
{
    public class EmailAsPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : IdentityUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (string.Equals(user.Email, password, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "EmailAsPassword",
                    Description = "Password cannot be the same as the email address"
                }));
            }
            if (user.Email?.Contains(password, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "EmailContainsPassword",
                    Description = "Password cannot be part of the email address"
                }));
            }
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
