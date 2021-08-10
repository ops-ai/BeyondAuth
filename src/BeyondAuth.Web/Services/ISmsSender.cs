using System.Threading.Tasks;
using static BeyondAuth.Web.Services.MessageSender;

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
        Task<SmsSendStatus> SendSmsAsync(string number, string message);
    }
}
