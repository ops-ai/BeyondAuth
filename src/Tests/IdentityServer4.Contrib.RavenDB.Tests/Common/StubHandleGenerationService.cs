using IdentityServer4.Services;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.RavenDB.Tests.Common
{
    public class StubHandleGenerationService : DefaultHandleGenerationService, IHandleGenerationService
    {
        public string Handle { get; set; }

        public new Task<string> GenerateAsync(int length)
        {
            if (Handle != null) return Task.FromResult(Handle);
            return base.GenerateAsync(length);
        }
    }
}
