namespace BlackstarSolar.AspNetCore.Identity.PwnedPasswords
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Options;
    using SharpPwned.NET;

    public class PwnedPasswordsValidator<TUser> : IPasswordValidator<TUser> where TUser : class
    {
        private readonly HaveIBeenPwnedRestClient client;

        private readonly PwnedPasswordsValidatorOptions options;

        public PwnedPasswordsValidator(IOptions<PwnedPasswordsValidatorOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            
            if (string.IsNullOrEmpty(options.Value.ApiKey))
                throw new ArgumentNullException(nameof(options.Value.ApiKey));

            this.options = options.Value;
            client = new HaveIBeenPwnedRestClient(options.Value.ApiKey, options.Value.UserAgent);
        }

        public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (!await client.IsPasswordPwned(password))
            {
                return IdentityResult.Success;
            }

            var result = IdentityResult.Failed(new IdentityError {Description = options.ErrorMessage});
            return result;
        }
    }
}