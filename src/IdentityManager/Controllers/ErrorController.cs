using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManager.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [AllowAnonymous]
        [Route("error")]
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                if (exceptionHandlerPathFeature.Error is FileNotFoundException)
                    Response.StatusCode = 404;

                try
                {
                    throw exceptionHandlerPathFeature.Error;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Uncaught exception {path}", exceptionHandlerPathFeature.Path);
                }
            }

            return Problem("Error processing request");
        }
    }
}
