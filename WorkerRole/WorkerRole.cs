using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Newtonsoft.Json;
using WorkerRole.Models;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        #region Windows Azure

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole entry point called");

            while (true)
            {
                Thread.Sleep(15000);
                Trace.TraceInformation("Waking up");

                BrokeredMessage message = QueueConnector.QueueClient.Receive();

                if (message != null)
                {
                    Trace.TraceInformation("Processing " + message.SequenceNumber.ToString());
                    ReaderDocument data = message.GetBody<ReaderDocument>();

                    try
                    {
                        Trace.TraceInformation("Parsing document");
                        var response = ParseUrl(data.URL);

                        Trace.TraceInformation("Converting document to Mobi format");
                        string filename = ConvertToMobi(response);

                        Trace.TraceInformation("Sending file to Kindle");
                        SendEmailToReader(data, filename);

                        message.Complete();

                        Trace.TraceInformation("Completed success");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);
                        message.Complete();

                        Trace.TraceWarning("Sending error message to user");
                        SendErrorEmail(data);

                        Trace.TraceWarning("Completed failure");
                    }
                }

                Trace.TraceInformation("Sleeping");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            QueueConnector.Initialize();

            return base.OnStart();
        }

        #endregion

        #region Send To Reader Server

        private ParseResponse ParseUrl(string url)
        {
            string key = CloudConfigurationManager.GetSetting("ReadabilityAPIKey");

            HttpWebRequest request = HttpWebRequest.Create("https://www.readability.com/api/content/v1/parser?token=" + key + "&url=" + url) as HttpWebRequest;
            request.Accept = "application/json";

            var response = request.GetResponse();

            Stream stream = response.GetResponseStream();
            UTF8Encoding encoding = new UTF8Encoding();
            StreamReader sr = new StreamReader(stream, encoding);

            JsonTextReader tr = new JsonTextReader(sr);
            ParseResponse data = new JsonSerializer().Deserialize<ParseResponse>(tr);

            return data;
        }

        private string ConvertToHtml(ParseResponse response)
        {
            string document =
@"<!DOCTYPE html>
<html><head>    <title>{0}</title>    <style>        body { font-size: 32px; line-height: 1.5em; font-family: Georgia, Times, serif; }        h1 { line-height: 1.3em; }        p { text-indent: 0; margin-top: 10px; }    </style></head>
<body>
    <h1>{0}</h1>
    <i>{1}</i>
    <br />
    {2}
</body>
";

            string meta = "By " + response.author + " (" + response.domain + ") on " + Convert.ToDateTime(response.date_published).ToString("MMMM d, yyyy");
            
            if (String.IsNullOrEmpty(response.date_published) == true)
                meta = "By " + response.author + " (" + response.domain + ")";

            if (String.IsNullOrEmpty(response.author) == true)
                meta = Convert.ToDateTime(response.date_published).ToString("MMMM d, yyyy");

            string html = String.Format(document, response.title, meta, response.content);
            
            return html;
        }

        private string ConvertToMobi(ParseResponse response)
        {
            string html = ConvertToHtml(response);

            string htmlFilename = FriendlyFilename(response.title) + ".html";
            if (File.Exists(htmlFilename) == true)
                File.Delete(htmlFilename);

            string mobiFilename = FriendlyFilename(response.title) + ".mobi";
            if (File.Exists(mobiFilename) == true)
                File.Delete(mobiFilename);

            File.WriteAllText(htmlFilename, html);
            
            Process kindleGen = new Process();

            kindleGen.StartInfo.UseShellExecute = false;
            kindleGen.StartInfo.RedirectStandardOutput = true;
            kindleGen.StartInfo.FileName = "kindlegen.exe";
            kindleGen.StartInfo.Arguments = string.Format("\"{0}\"", htmlFilename);

            kindleGen.Start();

            var output = kindleGen.StandardOutput.ReadToEnd();
            kindleGen.WaitForExit();

            return mobiFilename;
        }

        private string FriendlyFilename(string filename)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c.ToString(), "");
            }

            return filename;
        }

        private void SendEmailToReader(ReaderDocument data, string path)
        {
            string subject = "Send To Reader - Conversion Succeeded";
            if (data.EmailAddress.EndsWith("@kindle.com") == true)
                subject = "convert";

            MailMessage message = new MailMessage("Send To Reader <kindle@sendtoreader.net>", data.EmailAddress, subject, data.URL + "\n\nDocument sent using Send To Reader for Windows Phone. Check it out at http://sendtoreader.net.");

            Attachment file = new Attachment(path, MediaTypeNames.Application.Octet);
            message.Attachments.Add(file);

            SmtpClient client = new SmtpClient("smtp.sendgrid.net");

            string username = CloudConfigurationManager.GetSetting("SendGridUsername");
            string password = CloudConfigurationManager.GetSetting("SendGridPassword");

            client.Credentials = new NetworkCredential(username, password);

            client.Send(message);

            file.Dispose();
        }

        private void SendErrorEmail(ReaderDocument data)
        {
            string subject = "Send To Reader - Conversion Failed";

            MailMessage message = new MailMessage("Send To Reader <kindle@sendtoreader.net>", data.EmailAddress, subject, data.URL + "\n\nWe're sorry, but the website you submitted could not be converted to Kindle format. Please try your submission again at http://sendtoreader.net.");

            SmtpClient client = new SmtpClient("smtp.sendgrid.net");

            string username = CloudConfigurationManager.GetSetting("SendGridUsername");
            string password = CloudConfigurationManager.GetSetting("SendGridPassword");

            client.Credentials = new NetworkCredential(username, password);

            client.Send(message);
        }

        #endregion
    }
}
