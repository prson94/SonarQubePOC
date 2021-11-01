using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.extensions.mail
{
    public class DummyMailProvider : IMailProvider
    {
        public string ApiKey { get; set; }
        public string SubAccount { get; set; }

        public Task SendMessage(string fromName, string subject, string toEmail, string toName, string content, bool useHtml = false)
        {
            return Task.CompletedTask;
        }

        public Task SendMessage(string subject, string toEmail, string toName, string content, bool useHtml = false, string fromEmail = "no-reply@data3sixty.com", string fromName = "Data3Sixty Workflow")
        {
            return Task.CompletedTask;
        }

        public void SendMessage(string subject, string toEmail, string toName, Dictionary<string, string> templateTags, string templateID)
        {
            // do nothing.
        }
    }
}