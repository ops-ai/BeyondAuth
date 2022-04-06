using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Core
{
    public interface IOtacManager
    {
        Task<string> GenerateOtacAsync(ApplicationUser user, CancellationToken ct = default);

        Task<(IdentityResult, ApplicationUser?)> ValidateOtacAsync(string otac, CancellationToken ct = default);
    }
}
