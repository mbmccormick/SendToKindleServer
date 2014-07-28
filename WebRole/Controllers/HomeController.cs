using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using WorkerRole.Models;

namespace WebRole.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Get a NamespaceManager which allows you to perform management and
            // diagnostic operations on your Service Bus Queues.
            var namespaceManager = QueueConnector.CreateNamespaceManager();

            // Get the queue, and obtain the message count.
            var queue = namespaceManager.GetQueue(QueueConnector.QueueName);
            ViewBag.MessageCount = queue.MessageCount;

            return View();
        }
                
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(KindleDocument document)
        {
            if (ModelState.IsValid)
            {
                // Create a message from the order
                var message = new BrokeredMessage(document);

                // Submit the order
                QueueConnector.SendToKindleServerClient.Send(message);
                return RedirectToAction("Index");
            }
            else
            {
                return View(document);
            }
        }
    }
}