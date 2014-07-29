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
using TuesPechkin;
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

                        Trace.TraceInformation("Converting document to PDF");
                        var document = ConvertToPdf(response);

                        string filename = FriendlyFilename(response.title) + ".pdf";
                        if (File.Exists(filename) == true)
                            File.Delete(filename);

                        Trace.TraceInformation("Writing file to disk");
                        File.WriteAllBytes(filename, document);

                        Trace.TraceInformation("Sending file to Kindle");
                        SendEmailToReader(data, filename);

                        message.Complete();

                        Trace.TraceInformation("Completed success");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);
                        message.Complete(); // message.Abandon();

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

        private byte[] ConvertToPdf(ParseResponse response)
        {
            string style = "<style>body { font-size: 32px; line-height: 1.5em; font-family: Georgia, Times, serif; } h1 { line-height: 1.3em; }</style>\r\n\r\n";
            string title = "<h1>" + response.title + "</h1>\r\n\r\n";
            string meta = "<i>" + Convert.ToDateTime(response.date_published).ToString("MMMM d, yyyy") + " by " + response.author + "</i><br /><br />\r\n\r\n";

            if (String.IsNullOrEmpty(response.date_published) == true ||
                String.IsNullOrEmpty(response.author) == true)
            {
                meta = "";
            }

            HtmlToPdfDocument document = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    DocumentTitle = response.title,
                    ImageQuality = 100,
                    DPI = 1200,
                    ImageDPI = 1200,
                    Margins =
                    {
                        All = 0.5,
                        Unit = Unit.Inches
                    }
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlText = style + title + meta + response.content,
                        WebSettings =
                        {
                            PrintBackground = false,
                            PrintMediaType = true
                        }
                    }
                }
            };

            IPechkin converter = Factory.Create();
            byte[] data = converter.Convert(document);

            return data;
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

            MailMessage message = new MailMessage("Send To Reader <kindle@sendtoreader.cloudapp.net>", data.EmailAddress, subject, data.URL + "\n\nDocument sent using Send To Reader for Windows Phone. Check it out at http://sendtoreader.cloudapp.net.");

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

            MailMessage message = new MailMessage("Send To Reader <kindle@sendtoreader.cloudapp.net>", data.EmailAddress, subject, data.URL + "\n\nWe're sorry, but the website you submitted could not be converted to Kindle format. Please try your submission again at http://sendtoreader.cloudapp.net.");

            SmtpClient client = new SmtpClient("smtp.sendgrid.net");

            string username = CloudConfigurationManager.GetSetting("SendGridUsername");
            string password = CloudConfigurationManager.GetSetting("SendGridPassword");

            client.Credentials = new NetworkCredential(username, password);

            client.Send(message);
        }

        #endregion
    }
}
