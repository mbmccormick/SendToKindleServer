using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Pechkin;
using SendToKindleServer.Models;

namespace SendToKindleServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            var response = ParseUrl("https://medium.com/@matter.io/the-factory-os-2a57a1de1e64");

            var document = ConvertToHtml(response); // ConvertToPdf(response);

            string filename = FriendlyFilename(response.title);
            File.WriteAllText(filename + ".html", document);
        }

        static ParseResponse ParseUrl(string url)
        {
            HttpWebRequest request = HttpWebRequest.Create("https://www.readability.com/api/content/v1/parser?token=4c4159ab5bac5d6925d0d610332e577d5f57c45b&url=" + url) as HttpWebRequest;
            request.Accept = "application/json";

            var response = request.GetResponse();

            Stream stream = response.GetResponseStream();
            UTF8Encoding encoding = new UTF8Encoding();
            StreamReader sr = new StreamReader(stream, encoding);

            JsonTextReader tr = new JsonTextReader(sr);
            ParseResponse data = new JsonSerializer().Deserialize<ParseResponse>(tr);

            return data;
        }

        static byte[] ConvertToPdf(ParseResponse response)
        {
            GlobalConfig config = new GlobalConfig();
            config.SetDocumentTitle(response.title);
            
            string style = "<style>body { font-size: 300%; line-height: 1.5em; margin: 50px; } h1 { line-height: 1.3em; } img { width: 100%; }</style>\r\n\r\n";
            string title = "<h1>" + response.title + "</h1>\r\n\r\n";
            string meta = "<i>" + Convert.ToDateTime(response.date_published).ToString("MMMM dd, yyyy") + " by " + response.author + "</i>\r\n\r\n";

            byte[] data = new SimplePechkin(new GlobalConfig()).Convert(style + title + meta + response.content);

            return data;
        }

        static string ConvertToHtml(ParseResponse response)
        {
            string style = "<style>h1 { line-height: 1em; }</style>\r\n\r\n";
            string title = "<h1>" + response.title + "</h1>\r\n\r\n";
            string meta = "<i>" + Convert.ToDateTime(response.date_published).ToString("MMMM dd, yyyy") + " by " + response.author + "</i>\r\n\r\n";

            string data = "<!DOCTYPE html>\r\n<html>\r\n<head>\r\n<title>" + response.title + "</title>\r\n</head>\r\n<body>\r\n" + style + title + meta + response.content + "</body>\r\n</html>\r\n";

            return data;
        }

        static string FriendlyFilename(string filename)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c.ToString(), "");
            }

            return filename;
        }

        //static void SendToEmail(string emailAddress, string filePath, string title)
        //{
        //    string file = "data.xls";

        //    MailMessage message = new MailMessage("sendtokindle@mbmccormick.com", emailAddress, title, "Document sent using Sent To Kindle for Windows Phone.");

        //    // Create  the file attachment for this e-mail message.
        //    Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
            
        //    // Add time stamp information for the file.
        //    ContentDisposition disposition = data.ContentDisposition;
        //    disposition.CreationDate = System.IO.File.GetCreationTime(file);
        //    disposition.ModificationDate = System.IO.File.GetLastWriteTime(file);
        //    disposition.ReadDate = System.IO.File.GetLastAccessTime(file);
        //    // Add the file attachment to this e-mail message.
        //    message.Attachments.Add(data);

        //    //Send the message.
        //    SmtpClient client = new SmtpClient(server);
        //    // Add credentials if the SMTP server requires them.
        //    client.Credentials = CredentialCache.DefaultNetworkCredentials;

        //    try
        //    {
        //        client.Send(message);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exception caught in CreateMessageWithAttachment(): {0}",
        //              ex.ToString());
        //    }
        //    // Display the values in the ContentDisposition for the attachment.
        //    ContentDisposition cd = data.ContentDisposition;
        //    Console.WriteLine("Content disposition");
        //    Console.WriteLine(cd.ToString());
        //    Console.WriteLine("File {0}", cd.FileName);
        //    Console.WriteLine("Size {0}", cd.Size);
        //    Console.WriteLine("Creation {0}", cd.CreationDate);
        //    Console.WriteLine("Modification {0}", cd.ModificationDate);
        //    Console.WriteLine("Read {0}", cd.ReadDate);
        //    Console.WriteLine("Inline {0}", cd.Inline);
        //    Console.WriteLine("Parameters: {0}", cd.Parameters.Count);
        //    foreach (DictionaryEntry d in cd.Parameters)
        //    {
        //        Console.WriteLine("{0} = {1}", d.Key, d.Value);
        //    }
        //    data.Dispose();
        //}
    }
}
