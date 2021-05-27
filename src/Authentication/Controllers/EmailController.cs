using Authentication.Infrastructure;
using Authentication.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    /// <summary>
    /// Html email template render helper
    /// </summary>
    public class EmailController : Controller, IEmailService
    {
        private IRazorViewEngine _viewEngine;
        private ITempDataProvider _tempDataProvider;
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewEngine"></param>
        /// <param name="tempDataProvider"></param>
        /// <param name="serviceProvider"></param>
        public EmailController(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewName"></param>
        /// <param name="model"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<string> RenderPartialViewToString<TModel>(string viewName, TModel model, string notificationType)
        {
            var actionContext = GetActionContext();

            var viewEngineResult = _viewEngine.FindView(actionContext, $"EmailTemplates/{viewName}", false);

            if (!viewEngineResult.Success)
                throw new InvalidOperationException($"Couldn't find view '{viewName}");

            var view = viewEngineResult.View;
            if (model is EmailMessageModel)
                ViewData["EmailTo"] = (model as EmailMessageModel).To;
            ViewData["EmailType"] = notificationType;

            using (var output = new StringWriter())
            {
                var viewData = new ViewDataDictionary<TModel>(metadataProvider: new EmptyModelMetadataProvider(), modelState: new ModelStateDictionary())
                {
                    Model = model,

                };
                viewData.Add("EmailTo", (model as EmailMessageModel).To);
                viewData.Add("EmailType", notificationType);

                var viewContext = new ViewContext(actionContext, view, viewData,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    output,
                    new HtmlHelperOptions());

                await view.RenderAsync(viewContext);

                return output.ToString();
            }
        }

        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext
            {
                RequestServices = _serviceProvider
            };
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}