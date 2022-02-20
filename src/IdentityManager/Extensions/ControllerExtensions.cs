using Microsoft.AspNetCore.Mvc;

namespace IdentityManager.Extensions
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// Creates an Microsoft.AspNetCore.Mvc.PartialObjectResult object that produces an OK (206) response.
        /// </summary>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created Microsoft.AspNetCore.Mvc.PartialObjectResult for the response.</returns>
        public static PartialObjectResult Partial(this ControllerBase controller, object value) => new PartialObjectResult(value);
    }

    /// <summary>
    /// An Microsoft.AspNetCore.Mvc.ObjectResult that when executed performs content
    /// negotiation, formats the entity body, and will produce a Microsoft.AspNetCore.Http.StatusCodes.Status206PartialContent
    /// response if negotiation and formatting succeed.
    /// </summary>
    public class PartialObjectResult : ObjectResult
    {
        /// <summary>
        /// Initializes a new instance of the Microsoft.AspNetCore.Mvc.OkObjectResult class.
        /// </summary>
        /// <param name="value">The content to format into the entity body.</param>
        public PartialObjectResult(object value) : base(value) => StatusCode = StatusCodes.Status206PartialContent;
    }
}
