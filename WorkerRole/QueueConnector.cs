using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace WorkerRole
{
    public static class QueueConnector
    {
        public static QueueDescription Queue;
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
                Queue = namespaceManager.GetQueue("sendtoreader-dev");
                QueueClient = messagingFactory.CreateQueueClient("sendtoreader-dev");
            }
            else
            {
                Queue = namespaceManager.GetQueue("sendtoreader");
                QueueClient = messagingFactory.CreateQueueClient("sendtoreader");
            }
        }
    }
}