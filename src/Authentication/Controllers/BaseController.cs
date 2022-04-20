using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Raven.Client.Documents.Session;

namespace Authentication.Controllers
{
    public class BaseController : Controller
    {
        public BaseController(IAsyncDocumentSession dbSession)
        {
            this.DbSession = dbSession;

            // RavenDB best practice: during save, wait for the indexes to update.
            // This way, Post-Redirect-Get scenarios won't be affected by stale indexes.
            // For more info, see https://ravendb.net/docs/article-page/4.2/csharp/client-api/session/saving-changes
            this.DbSession.Advanced.WaitForIndexesAfterSaveChanges(timeout: TimeSpan.FromSeconds(5), throwOnTimeout: false);
        }

        public IAsyncDocumentSession DbSession { get; private set; }

        /// <summary>
        /// Executes the action. If no error occurred, any changes made in the RavenDB document session will be saved.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next.Invoke();
            if (executedContext.Exception == null)
            {
                await DbSession.SaveChangesAsync();
            }
        }
    }
}
