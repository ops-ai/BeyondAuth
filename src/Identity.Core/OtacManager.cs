using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Raven.Client;
using Raven.Client.Documents.Session;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Core
{
    public class OtacManager : IOtacManager
    {
        private readonly IAsyncDocumentSession _session;

        public OtacManager(IAsyncDocumentSession session)
        {
            _session = session;
        }

        public async Task<string> GenerateOtacAsync(ApplicationUser user, CancellationToken ct = default)
        {
            var code = CryptoRandom.CreateUniqueId();
            var hash = Hashing.GetStringSha256Hash(code);

            var otac = new Otac
            {
                Id = $"Otacs/{hash}",
                UserId = user.Id!
            };
            await _session.StoreAsync(otac);
            _session.Advanced.GetMetadataFor(otac)[Constants.Documents.Metadata.Expires] = DateTime.UtcNow.AddMinutes(1);

            await _session.SaveChangesAsync();

            return code;
        }

        public async Task<(IdentityResult, ApplicationUser?)> ValidateOtacAsync(string code, CancellationToken ct = default)
        {
            var hash = Hashing.GetStringSha256Hash(code);
            var otac = await _session.Include<Otac>(t => t.UserId).LoadAsync<Otac>($"Otacs/{hash}", ct);
            if (otac != null)
            {
                _session.Delete(otac);
                await _session.SaveChangesAsync(ct);

                if (otac.CreatedOnUtc > DateTime.UtcNow.AddMinutes(-1))
                    return (IdentityResult.Success, await _session.LoadAsync<ApplicationUser>(otac.UserId, ct));
            }

            return (IdentityResult.Failed(new IdentityError { Code = "Invalid", Description = "Code was not found or it expired" }), null);
        }
    }
}
