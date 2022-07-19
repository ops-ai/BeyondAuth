﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IdentityManager.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        public ErrorController()
        {

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
            }

            return Problem("Error processing request");
        }
    }
}
