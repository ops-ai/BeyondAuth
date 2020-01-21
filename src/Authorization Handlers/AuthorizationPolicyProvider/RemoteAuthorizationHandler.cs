using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using AuthorizationServer;
using System;
using Microsoft.AspNetCore.Http;
using PolicyServer.Core.Entities;
using System.Linq;

namespace BeyondAuth.PolicyProvider
{
    public class RemoteAuthorizationHandler : IAuthorizationHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RemoteAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            var nonce = Guid.NewGuid().ToString();

            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Authorization.AuthorizationClient(channel);

            var authorizationRequest = new AuthorizationRequest
            {
                Nonce = nonce,
                Policy = null,
                Resource = context.Resource.ToString(),
                Sub = context.User.Identity.Name
            };

            authorizationRequest.Claims.Add(context.User.Claims.ToDictionary(t => t.Type, t => t.Value));

            foreach (var headerKey in _httpContextAccessor.HttpContext.Request.Headers)
                foreach (var headerValue in headerKey.Value)
                    authorizationRequest.Headers.Add(headerKey.Key, headerValue);

            //TODO: Figure out mapping
            foreach (var req in context.PendingRequirements)
                authorizationRequest.Requirements.Add(new AuthorizationRequest.Types.Requirement());

            var authorizationResponse = await client.AuthorizeAsync(authorizationRequest);

            if (authorizationResponse.Nonce != nonce)
                throw new AccessViolationException("Authorization response nonce doesn't match");

            if (authorizationResponse.Decision == AuthorizationResponse.Types.Decision.Allowed)
                foreach (AuthorizationRequirement req in context.PendingRequirements)
                    context.Succeed(req);
            else if (authorizationResponse.Decision == AuthorizationResponse.Types.Decision.Denied)
                context.Fail();

            //TODO: Handle insufficient context
        }
    }
}
