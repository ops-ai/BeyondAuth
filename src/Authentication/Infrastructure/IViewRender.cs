namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface IViewRender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="name"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        string Render<TModel>(string name, TModel model);
    }
}
