using System.Threading.Tasks;

namespace Manafont.Web
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage) {
            throw new System.NotImplementedException();
        }
    }
}