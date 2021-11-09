using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    public interface IMailProvider
    {
        string ApiKey { get; set; }
        string SubAccount { get; set; }

        Task SendMessage(string fromName, string subject, string toEmail, string toName, string content, bool useHtml = false);
        Task SendMessage(string subject, string toEmail, string toName, string content, bool useHtml = false, string fromEmail = "no-reply@data3sixty.com", string fromName = "Data3Sixty Workflow");
        void SendMessage(string subject, string toEmail, string toName, Dictionary<string, string> templateTags, string templateID);
    }
}
