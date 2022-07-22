using Authentication.Domain;
using Authentication.Filters;
using Authentication.Models.Security;
using Identity.Core;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using UAParser;

namespace Authentication.Controllers
{
    [Route("{controller}")]
    [SecurityHeaders]
    [Authorize]
    public class SecurityController : BaseController
    {
        private readonly IAsyncDocumentSession _dbSession;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SecurityController(
            IAsyncDocumentSession dbSession,
            IIdentityServerInteractionService interaction,
            IWebHostEnvironment environment,
            ILogger<HomeController> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager) : base(dbSession)
        {
            _dbSession = dbSession;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        [Route("")]
        [Authorize]
        public async Task<IActionResult> Index(CancellationToken ct = default)
        {
            var sub = User.GetSubjectId();

            var sessions = await _dbSession.Query<UserSession>().Where(t => t.UserId == sub).ToListAsync(ct);
            var model = new SecurityModel();
            foreach (var userSession in sessions.OrderByDescending(t => t.LastSeenOnUtc))
            {
                var uaParser = Parser.GetDefault();
                var useragent = uaParser.Parse(userSession.UserAgent);

                model.Sessions.Add(new SessionInfoModel
                {
                    BrowserFamily = useragent.UA.Family,
                    BrowserVersion = useragent.UA.Major,
                    LastSeen = userSession.LastSeenOnUtc,
                    OS = useragent.OS.Family,
                    DeviceFamily = useragent.Device.Family
                });
            }

            return View(model);
        }
    }
}