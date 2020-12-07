using System.Threading.Tasks;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <param name="txtMessage"></param>
        /// <param name="cc">Email to CC</param>
        /// <returns></returns>
        Task SendEmailAsync(string to, string subject, string htmlMessage, string txtMessage, string cc = null);
    }
}
