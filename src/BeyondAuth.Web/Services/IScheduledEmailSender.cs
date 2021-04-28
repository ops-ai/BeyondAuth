using System.Threading.Tasks;

namespace BeyondAuth.Web.Services
{
    public interface IScheduledEmailSender
    {
        Task SendEmailAsync(string id);
    }
}
