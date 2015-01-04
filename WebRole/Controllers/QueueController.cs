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

            while (QueueConnector.QueueClient.Peek() != null)
            {
                BrokeredMessage message = QueueConnector.QueueClient.Receive();

                message.Complete();
            }

            ViewBag.DeletedMessageCount = ViewBag.MessageCount - QueueConnector.GetQueue().MessageCount;

            return View();
        }
    }
}