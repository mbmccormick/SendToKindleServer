using System.Web.Mvc;
using Microsoft.ServiceBus.Messaging;
using WorkerRole.Models;

namespace WebRole.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.MessageCount = QueueConnector.GetQueue().MessageCountDetails.ActiveMessageCount;

            return View();
        }

        [HttpPost]
        public ActionResult Index(ReaderDocument document)
        {
            if (ModelState.IsValid)
            {
                var message = new BrokeredMessage(document);
                QueueConnector.QueueClient.Send(message);

                return RedirectToAction("Index");
            }
            else
            {
                return View(document);
            }
        }
    }
}