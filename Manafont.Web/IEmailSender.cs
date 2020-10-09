using System.Threading.Tasks;

namespace Manafont.Web
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}