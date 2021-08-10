using System.Threading.Tasks;
using static Authentication.Infrastructure.MessageSender;

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
        Task<SmsSendStatus> SendSmsAsync(string number, string message);
    }
}
