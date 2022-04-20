namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="viewName"></param>
        /// <param name="model"></param>
        /// <param name="notificationType"></param>
        /// <returns></returns>
        Task<string> RenderPartialViewToString<TModel>(string viewName, TModel model, string notificationType);
    }
}
