using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System.Collections.Generic;
using System.Net.Mail;
namespace Helpers
{
    public class SendMail
    {
        public string SendEMailWithMailKit(List<string> mailTos, string subject, string smtpServer, string sender, string content,
            string password, string header, string footer, int portNum, string title, string greeting, byte[] fileBytes)
        {
            try
            {
                var email = new MimeKit.MimeMessage();

                email.From.Add(MimeKit.MailboxAddress.Parse(sender));
                foreach (var mail in mailTos)
                {
                    email.To.Add(MimeKit.MailboxAddress.Parse(mail));
                }
                email.Subject = subject;
                
                var SmtpServer = new MailKit.Net.Smtp.SmtpClient();
                var builder = new BodyBuilder
                {
                    HtmlBody = content
                };

                using var stream = new MemoryStream(fileBytes);
                builder.Attachments.Add("PurchaseSheet.pdf", stream, ContentType.Parse("application/pdf"));
                email.Body = builder.ToMessageBody();                
                SmtpServer.Connect(smtpServer, portNum, SecureSocketOptions.StartTls);
                SmtpServer.Authenticate(sender, password);

                SmtpServer.Send(email);
                SmtpServer.Disconnect(true);
                return "Mails send successfully to all clients";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
