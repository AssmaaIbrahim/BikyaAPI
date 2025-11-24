using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class GmailServiceHelper
    {
        private static readonly string[] Scopes = { GmailService.Scope.GmailSend };
        private static readonly string ApplicationName = "Bikya Mailer";

        public static async Task<GmailService> GetGmailServiceAsync()
        {
            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
            var credPath = "token.json";

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(credPath, true));

            return new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });
        }

        public static async Task SendEmailAsync(string to, string subject, string body)
        {
            var gmailService = await GetGmailServiceAsync();

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("Bikya", "ahka6211@gmail.com")); // استخدم نفس Gmail
            emailMessage.To.Add(MailboxAddress.Parse(to));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart("html") { Text = body };

            using var stream = new MemoryStream();
            emailMessage.WriteTo(stream);
            var rawMessage = Convert.ToBase64String(stream.ToArray())
                .Replace("+", "-").Replace("/", "_").Replace("=", "");

            var message = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };
            await gmailService.Users.Messages.Send(message, "me").ExecuteAsync();
        }
    }
}
