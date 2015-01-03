using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Microsoft.ServiceBus.Messaging;
using WorkerRole.Models;

namespace WebRole.Controllers
{
    public class QueueController : Controller
    {
        public ActionResult Delete()
        {
            ViewBag.MessageCount = QueueConnector.GetQueue().MessageCount;

            BrokeredMessage message = QueueConnector.QueueClient.Receive(new TimeSpan(0, 0, 3));

            while (message != null)
            {
                message.Complete();

                message = QueueConnector.QueueClient.Receive(new TimeSpan(0, 0, 3));
            }

            ViewBag.DeletedMessageCount = ViewBag.MessageCount - QueueConnector.GetQueue().MessageCount;

            return View();
        }
    }
}