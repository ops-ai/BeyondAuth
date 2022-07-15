using Finbuckle.MultiTenant;
using Identity.Core;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Authentication.Services
{
    public class ViewLocationExpander : IViewLocationExpander
    {
        private const string THEME_KEY = "theme";

        public void PopulateValues(ViewLocationExpanderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var tenantSettings = context.ActionContext.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;

            // context.Values[THEME_KEY] = tenantSettings.BrandingOptions.Theme;
            context.Values[THEME_KEY] = "Modern";
        }

        public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (viewLocations == null)
            {
                throw new ArgumentNullException(nameof(viewLocations));
            }
            if (context.Values.TryGetValue(THEME_KEY, out var theme))
            {
                viewLocations = new[] {
                        $"/Themes/{theme}/{{1}}/{{0}}.cshtml",
                        $"/Themes/{theme}/Shared/{{0}}.cshtml",
                    }
                    .Concat(viewLocations);
            }

            return viewLocations;
        }
    }
}
