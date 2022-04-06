using Cryptography;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Raven.Client;
using Raven.Client.Documents;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.Core
{
    public class OtacManager : IOtacManager
    {
        private readonly IDocumentStore _store;

        public OtacManager(IDocumentStore store)
        {
            _store = store;
        }

        public async Task<string> GenerateOtacAsync(ApplicationUser user, CancellationToken ct = default)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var code = CryptoRandom.CreateUniqueId();
                var hash = Hashing.GetStringSha256Hash(code);

                var otac = new Otac
                {
                    Id = $"Otacs/{hash}",
                    UserId = user.Id!
                };
                await session.StoreAsync(otac);
                session.Advanced.GetMetadataFor(otac)[Constants.Documents.Metadata.Expires] = DateTime.UtcNow.AddMinutes(1);

                await session.SaveChangesAsync();

                return hash;
            }
        }

        public async Task<(IdentityResult, ApplicationUser?)> ValidateOtacAsync(string code, CancellationToken ct = default)
        {
            using (var session = _store.OpenAsyncSession())
            {
                var hash = Hashing.GetStringSha256Hash(code);
                var otac = await session.Include<Otac>(t => t.UserId).LoadAsync<Otac>($"Otacs/{hash}", ct);
                if (otac != null)
                {
                    session.Delete(otac);
                    await session.SaveChangesAsync(ct);

                    if (otac.CreatedOnUtc > DateTime.UtcNow.AddMinutes(-1))
                        return (IdentityResult.Success, await session.LoadAsync<ApplicationUser>(otac.UserId, ct));
                }

                return (IdentityResult.Failed(new IdentityError { Code = "Invalid", Description = "Code was not found or it expired" }), null);
            }
        }
    }
}
