using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using TuesPechkin;
using WorkerRole.Models;

namespace WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        // The name of your queue
        const string QueueName = "SendToKindleServer";
        QueueClient Client;

        #region Windows Azure

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("WorkerRole entry point called");

            while (true)
            {
                Thread.Sleep(10000);
                Trace.TraceInformation("Waking up");

                BrokeredMessage message = Client.Receive();

                if (message != null)
                {
                    Trace.TraceInformation("Processing " + message.SequenceNumber.ToString());

                    KindleDocument data = message.GetBody<KindleDocument>();

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
                        SendToKindle(data.EmailAddress, filename);

                        // Remove message from queue
                        message.Complete();

                        Trace.TraceInformation("Done");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.Message);

                        // Indicate a problem, unlock message in queue
                        message.Complete(); // message.Abandon();

                        try
                        {
                            SendErrorEmail(data.EmailAddress);
                        }
                        catch (Exception)
                        {
                            // do nothing
                        }
                    }
                }

                Trace.TraceInformation("Sleeping");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Initialize the connection to Service Bus Queue
            Client = QueueClient.CreateFromConnectionString(connectionString, QueueName);

            return base.OnStart();
        }

        #endregion

        #region Send To Kindle Server

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

        private void SendToKindle(string emailAddress, string filePath)
        {
            string subject = "Send To Kindle - Conversion Succeeded";
            if (emailAddress.EndsWith("@kindle.com") == true)
                subject = "convert";

            MailMessage message = new MailMessage("converter@sendtokindle.cloudapp.net", emailAddress, subject, "Document sent using Send To Kindle for Windows Phone. Check it out at http://sendtokindle.cloudapp.net.");

            // Create  the file attachment for this e-mail message.
            Attachment data = new Attachment(filePath, MediaTypeNames.Application.Octet);

            // Add time stamp information for the file.
            ContentDisposition disposition = data.ContentDisposition;
            disposition.CreationDate = System.IO.File.GetCreationTime(filePath);
            disposition.ModificationDate = System.IO.File.GetLastWriteTime(filePath);
            disposition.ReadDate = System.IO.File.GetLastAccessTime(filePath);

            // Add the file attachment to this e-mail message.
            message.Attachments.Add(data);

            //Send the message.
            SmtpClient client = new SmtpClient("smtp.sendgrid.net");

            // Add credentials if the SMTP server requires them.
            string username = CloudConfigurationManager.GetSetting("SendGridUsername");
            string password = CloudConfigurationManager.GetSetting("SendGridPassword");

            client.Credentials = new NetworkCredential(username, password);

            client.Send(message);

            data.Dispose();
        }

        private void SendErrorEmail(string emailAddress)
        {
            MailMessage message = new MailMessage("converter@sendtokindle.cloudapp.net", emailAddress, "Send To Kindle - Conversion Failed", "We're sorry, but the website you submitted could not be converted to Kindle format. Please try your submission again at http://sendtokindle.cloudapp.net.");

            //Send the message.
            SmtpClient client = new SmtpClient("smtp.sendgrid.net");

            // Add credentials if the SMTP server requires them.
            string username = CloudConfigurationManager.GetSetting("SendGridUsername");
            string password = CloudConfigurationManager.GetSetting("SendGridPassword");

            client.Credentials = new NetworkCredential(username, password);

            client.Send(message);
        }

        #endregion
    }
}
