using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ShowroomCar.Api.Services
{
    public class MailService
    {
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendPurchaseOrderAsync(string to, string subject, string htmlBody)
        {
            var mailSection = _config.GetSection("Mail");

            var smtp = new SmtpClient
            {
                Host = mailSection["SmtpServer"],
                Port = int.Parse(mailSection["Port"]),
                EnableSsl = bool.Parse(mailSection["EnableSsl"]),
                Credentials = new NetworkCredential(
                    mailSection["User"],
                    mailSection["Password"]
                )
            };

            var message = new MailMessage
            {
                From = new MailAddress(mailSection["From"], mailSection["DisplayName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(to);

            await smtp.SendMailAsync(message);
        }
    }
}
