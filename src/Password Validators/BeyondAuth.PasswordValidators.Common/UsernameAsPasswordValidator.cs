using Microsoft.AspNetCore.Identity;
using Raven.Identity;
using System;
using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Common
{
    public class UsernameAsPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : IdentityUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (string.Equals(user.UserName, password, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordUsernameAsPassword",
                    Description = "Password cannot be the same as the username"
                }));
            }
            return Task.FromResult(IdentityResult.Success);
        }
    }
}
