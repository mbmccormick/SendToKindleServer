using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace WorkerRole
{
    public static class QueueConnector
    {
        public static QueueClient QueueClient;

        public static NamespaceManager CreateNamespaceManager()
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            return NamespaceManager.CreateFromConnectionString(connectionString);
        }

        public static void Initialize()
        {
            ServiceBusEnvironment.SystemConnectivity.Mode = ConnectivityMode.Http;

            var namespaceManager = QueueConnector.CreateNamespaceManager();

            var messagingFactory = MessagingFactory.Create(namespaceManager.Address, namespaceManager.Settings.TokenProvider);

            if (Debugger.IsAttached)
            {
                QueueClient = messagingFactory.CreateQueueClient("sendtoreader-dev");
            }
            else
            {
                QueueClient = messagingFactory.CreateQueueClient("sendtoreader");
            }
        }

        public static QueueDescription GetQueue()
        {
            var namespaceManager = QueueConnector.CreateNamespaceManager();

            if (Debugger.IsAttached)
            {
                return namespaceManager.GetQueue("sendtoreader-dev");
            }
            else
            {
                return namespaceManager.GetQueue("sendtoreader");
            }
        }
    }
}