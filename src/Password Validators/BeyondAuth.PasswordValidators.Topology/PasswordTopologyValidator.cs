using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Topology
{
    public class PasswordTopologyValidator<TUser> : IPasswordValidator<TUser> where TUser : class
    {
        private readonly IPasswordTopologyProvider _topologyProvider;
        private readonly IOptions<PasswordTopologyValidatorOptions> _topologyValidatorOptions;

        public PasswordTopologyValidator(IPasswordTopologyProvider topologyProvider, IOptions<PasswordTopologyValidatorOptions> topologyValidatorOptions)
        {
            _topologyProvider = topologyProvider;
            _topologyValidatorOptions = topologyValidatorOptions;
        }

        public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
        {
            if (await _topologyProvider.GetTopologyCount(password) >= _topologyValidatorOptions.Value.Threshold)
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordTopology",
                    Description = _topologyValidatorOptions.Value.ErrorMessage
                });

            return IdentityResult.Success;
        }
    }
}
