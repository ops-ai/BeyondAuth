namespace Authentication.Infrastructure
{
    public class ErrorReporterMiddleware
    {
        private readonly RequestDelegate RequestDelegate;

        public ErrorReporterMiddleware(RequestDelegate requestDelegate)
        {
            RequestDelegate = requestDelegate ?? throw new ArgumentNullException(nameof(requestDelegate));
        }

        public async Task Invoke(HttpContext httpContext, ILogger<ErrorReporterMiddleware> logger)
        {
            try
            {
                await RequestDelegate(httpContext);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Uncaught error");
                throw;
            }
        }
    }
}
