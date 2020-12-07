using System.Threading.Tasks;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISmsSender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendSmsAsync(string number, string message);
    }
}
