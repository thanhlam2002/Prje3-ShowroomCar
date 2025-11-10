using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace ShowroomCar.Api.Services
{
    public class MailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<MailService> _logger;

        public MailService(IConfiguration config, ILogger<MailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendPurchaseOrderAsync(string to, string subject, string htmlBody)
        {
            try
            {
                var host = _config["Mail:SmtpHost"];
                var port = int.Parse(_config["Mail:SmtpPort"]);
                var user = _config["Mail:User"];
                var pass = _config["Mail:Pass"];
                var from = _config["Mail:From"];

                using var smtp = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(user, pass)
                };

                var msg = new MailMessage(from, to, subject, htmlBody)
                {
                    IsBodyHtml = true
                };

                await smtp.SendMailAsync(msg);
                _logger.LogInformation($"📨 Mail sent to {to} : {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send mail to {to}");
            }
        }
    }
}
