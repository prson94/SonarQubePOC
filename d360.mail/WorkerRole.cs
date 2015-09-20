using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Simple.MailServer.Smtp;
using Simple.MailServer.Smtp.Config;
using System.IO;
using System.Text;
using Simple.MailServer.Logging;

namespace d360.mail
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        
        private const string RootMailDir = @"C:\Temp\Mail\";

        public override void Run()
        {
            Trace.TraceInformation("d360.mail is running");

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("d360.mail has been started");

            return result;
        }

        public override void OnStop()
        {
            Trace.TraceInformation("d360.mail is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("d360.mail has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            //// TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                // to inject your own logging implement IMailServerLogger
                MailServerLogger.Set(new MailServerConsoleLogger(MailServerLogLevel.Debug));

                using (StartSmtpServer())
                {
                    await Task.Delay(1000);
                }
            }
        }

        private static SmtpServer StartSmtpServer()
        {
            var smtpServer = new SmtpServer
            {
                Configuration =
                {
                    DefaultGreeting = "Simple.MailServer Example"
                }
            };
            smtpServer.DefaultResponderFactory =
                new DefaultSmtpResponderFactory<ISmtpServerConfiguration>(smtpServer.Configuration)
                {
                    DataResponder = new ExampleDataResponder(smtpServer.Configuration, RootMailDir)
                    // ... inject other responders here as needed (or leave default)
                };

            smtpServer.BindAndListenTo(IPAddress.Loopback, 25);
            return smtpServer;
        }
    }

    class ExampleDataResponder : DefaultSmtpDataResponder<ISmtpServerConfiguration>
    {
        private readonly string _mailDir;

        public ExampleDataResponder(ISmtpServerConfiguration configuration, string mailDir)
            : base(configuration)
        {
            _mailDir = mailDir;
            EnsureDirExists(mailDir);
        }

        private static void EnsureDirExists(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        public override SmtpResponse DataStart(SmtpSessionInfo sessionInfo)
        {
            Console.WriteLine("Start receiving mail: {0}", GetFileNameFromSessionInfo(sessionInfo));
            return SmtpResponse.DataStart;
        }

        private string GetFileNameFromSessionInfo(SmtpSessionInfo sessionInfo)
        {
            var fileName = sessionInfo.CreatedTimestamp.ToString("yyyy-MM-dd_HHmmss_fff") + ".eml";
            var fullName = Path.Combine(_mailDir, fileName);
            return fullName;
        }

        public override SmtpResponse DataLine(SmtpSessionInfo sessionInfo, byte[] lineBuf)
        {
            var fileName = GetFileNameFromSessionInfo(sessionInfo);

            Console.WriteLine("{0} <<< {1}", fileName, Encoding.UTF8.GetString(lineBuf));

            using (var stream = File.OpenWrite(fileName))
            {
                stream.Seek(0, SeekOrigin.End);
                stream.Write(lineBuf, 0, lineBuf.Length);

                stream.WriteByte(13);
                stream.WriteByte(10);
            }

            return SmtpResponse.None;
        }

        public override SmtpResponse DataEnd(SmtpSessionInfo sessionInfo)
        {
            var fileName = GetFileNameFromSessionInfo(sessionInfo);
            var size = GetFileSize(fileName);

            Console.WriteLine("Mail received ({0} bytes): {1}", size, fileName);

            var successMessage = String.Format("{0} bytes received", size);
            var response = SmtpResponse.OK.CloneAndChange(successMessage);

            return response;
        }

        private long GetFileSize(string fileName)
        {
            return new FileInfo(fileName).Length;
        }
    }
}
