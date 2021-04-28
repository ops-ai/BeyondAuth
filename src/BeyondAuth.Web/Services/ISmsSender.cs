using System.Threading.Tasks;

namespace BeyondAuth.Web.Services
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
