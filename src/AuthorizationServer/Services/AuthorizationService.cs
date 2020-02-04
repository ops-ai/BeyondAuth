using Grpc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AuthorizationServer
{
    public class AuthorizationService : Authorization.AuthorizationBase
    {
        private readonly ILogger<AuthorizationService> _logger;
        public AuthorizationService(ILogger<AuthorizationService> logger) => _logger = logger;

        public override Task<AuthorizationResponse> Authorize(AuthorizationRequest request, ServerCallContext context)
        {
            return Task.FromResult(new AuthorizationResponse
            {

            });
        }
    }
}
