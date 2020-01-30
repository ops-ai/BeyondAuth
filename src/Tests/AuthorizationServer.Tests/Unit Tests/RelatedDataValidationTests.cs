using BeyondAuth.RelatedDataValidation;
using BeyondAuth.RelatedDataValidation.Indices;
using BeyondAuth.RelatedDataValidation.Requirements;
using Cryptography;
using Divergic.Logging.Xunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;
using Raven.Client.Documents;
using Raven.TestDriver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AuthorizationServer.Tests.Unit_Tests
{
    public class RelatedDataValidationTests : RavenTestDriver
    {
        protected readonly ILoggerFactory _loggerFactory;
        private readonly IAuthorizationHandler _authorizationHandler;
        private readonly IRelatedDataAuthorizationService _relatedDataAuthorizationService;
        
        private readonly IDocumentStore _documentStore;

        public RelatedDataValidationTests(ITestOutputHelper output)
        {
            _loggerFactory = LogFactory.Create(output);
            _documentStore = GetDocumentStore();
            _relatedDataAuthorizationService = new RelatedDataAuthorizationService(_documentStore);
            _authorizationHandler = new RelatedDataAuthorizationHandler(_relatedDataAuthorizationService);

            new Index_RelatedDataAgg().Execute(_documentStore);
        }

        [Fact(DisplayName = "Authorization Handler calls validation service")]
        public async Task AuthorizationHandlerCallsValidationService()
        {
            var fakeRelatedDataAuthorizationService = new Mock<IRelatedDataAuthorizationService>();
            fakeRelatedDataAuthorizationService.Setup(t => t.ValidateResource(It.IsAny<IRelatedDataEntity>())).ReturnsAsync(true);
            var authorizationHandler = new RelatedDataAuthorizationHandler(fakeRelatedDataAuthorizationService.Object);

            var resource = new RelatedDataEntity { };
            var authorizationContext = new AuthorizationHandlerContext(new[] { new RelatedDataRequirement() }, new System.Security.Claims.ClaimsPrincipal(), resource);
            await authorizationHandler.HandleAsync(authorizationContext);

            fakeRelatedDataAuthorizationService.Verify(t => t.ValidateResource(resource), Times.Once());
            fakeRelatedDataAuthorizationService.VerifyNoOtherCalls();

            Assert.True(authorizationContext.HasSucceeded);
        }

        [Fact(DisplayName = "Authorization Handler doesn't call validation service if data invalid")]
        public async Task AuthorizationHandlerDataInvalid()
        {
            var fakeRelatedDataAuthorizationService = new Mock<IRelatedDataAuthorizationService>();
            var authorizationHandler = new RelatedDataAuthorizationHandler(fakeRelatedDataAuthorizationService.Object);

            var resource = "invalid type";
            var authorizationContext = new AuthorizationHandlerContext(new[] { new RelatedDataRequirement() }, new System.Security.Claims.ClaimsPrincipal(), resource);
            await authorizationHandler.HandleAsync(authorizationContext);

            fakeRelatedDataAuthorizationService.VerifyNoOtherCalls();

            Assert.False(authorizationContext.HasSucceeded);
        }

        [Fact(DisplayName = "RelatedDataAuthorizationService should be able to retrieve stored data")]
        public async Task RelatedDataAuthorizationService()
        {
            var hash1 = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test")));

            var resource1 = new RelatedDataEntity { Sha256HashCode = hash1 };
            resource1.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource1);
            var resource2 = await _relatedDataAuthorizationService.GetResource(hash1);

            Assert.Equal(resource1.Sha256HashCode, resource2.Sha256HashCode);
            Assert.Equal(resource1.RelSha256HashCode, resource2.RelSha256HashCode);
            Assert.Equal(resource1.Data, resource2.Data);
        }

        [Fact(DisplayName = "RelatedDataAuthorizationService should be able to retrieve stored data by properties")]
        public async Task RelatedDataAuthorizationServiceByProperties()
        {
            var hash1 = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test")));

            var resource1 = new RelatedDataEntity { Sha256HashCode = hash1 };
            resource1.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource1.Sha256HashCode, resource1.RelSha256HashCode, resource1.Data);
            var resource2 = await _relatedDataAuthorizationService.GetResource(hash1);

            Assert.Equal(resource1.Sha256HashCode, resource2.Sha256HashCode);
            Assert.Equal(resource1.RelSha256HashCode, resource2.RelSha256HashCode);
            Assert.Equal(resource1.Data, resource2.Data);
        }

        [Fact(DisplayName = "RelatedDataAuthorizationService should be able to retrieve stored validation rules")]
        public async Task StoreValidationRules()
        {
            var rule1 = new RelatedDataValidationRule { Name = "rule1" };
            rule1.Conditions.Add("Organization", "Bank1");
            rule1.Requirements.Add(new SingleValueRequirementRule { PropertyName = "Organization" });

            var ruleId = await _relatedDataAuthorizationService.AddValidationrule(rule1);
            var rule2 = await _relatedDataAuthorizationService.GetValidationRule(ruleId);

            Assert.Equal(rule1.Name, rule2.Name);
            Assert.Equal(rule1.Conditions, rule2.Conditions);
            Assert.Equal(rule1.Requirements, rule2.Requirements);
        }

        [Fact(DisplayName = "RelatedDataAuthorizationService validator should retrieve aggregate data")]
        public async Task ValidatorShouldRetrieveAggregateData()
        {
            var resource1 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test"))) };
            resource1.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource1);

            var resource2 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test"))) };
            resource2.Data.Add("Organization", new HashSet<string> { "Bank2" });
            await _relatedDataAuthorizationService.AddResource(resource2);

            var resource3 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test3"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))) };
            resource3.Data.Add("Organization", new HashSet<string> { "Bank3" });
            await _relatedDataAuthorizationService.AddResource(resource3);

            var resource4 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test4"))) };
            resource4.Data.Add("Dept", new HashSet<string> { "Dept1" });
            await _relatedDataAuthorizationService.AddResource(resource4);

            WaitForIndexing(_documentStore);

            var newResource = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test3"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))) };
            newResource.Data.Add("Organization", new HashSet<string> { "Bank3" });

            var relatedData = await _relatedDataAuthorizationService.GetRelatedEntityData(Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test"))));

            Assert.Equal(new Dictionary<string, HashSet<string>> { { "Organization", new HashSet<string> { "Bank1", "Bank2", "Bank3" } } }, relatedData);

            var valid = await _relatedDataAuthorizationService.ValidateResource(newResource);

            Assert.True(valid);
        }

        [Fact(DisplayName = "RelatedDataAuthorizationService single value validator")]
        public async Task ValidatorShouldValidateUsingAggregateData()
        {
            var rule1 = new RelatedDataValidationRule { Name = "rule1" };
            rule1.Requirements.Add(new SingleValueRequirementRule { PropertyName = "Organization" });

            await _relatedDataAuthorizationService.AddValidationrule(rule1);

            var resource1 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test"))) };
            resource1.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource1);


            var resource2 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test"))) };
            resource2.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource2);

            var resource3 = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test3"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))) };
            resource3.Data.Add("Organization", new HashSet<string> { "Bank1" });
            await _relatedDataAuthorizationService.AddResource(resource3);

            WaitForIndexing(_documentStore);

            var newResource = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test3"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))) };
            newResource.Data.Add("Organization", new HashSet<string> { "Bank3" });

            var valid = await _relatedDataAuthorizationService.ValidateResource(newResource);

            Assert.False(valid);

            var newValidResource = new RelatedDataEntity { Sha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test3"))), RelSha256HashCode = Convert.ToBase64String(Hashing.ComputeHashSha256(Encoding.UTF8.GetBytes("test2"))) };
            newValidResource.Data.Add("Organization", new HashSet<string> { "Bank1" });

            var validResource = await _relatedDataAuthorizationService.ValidateResource(newValidResource);

            Assert.True(validResource);
        }
    }
}
