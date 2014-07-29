using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace WebRole
{
    public static class QueueConnector
    {
        // Thread-safe. Recommended that you cache rather than recreating it
        // on every request.
        public static QueueClient sendtoreaderClient;

        // The name of your queue
        public const string QueueName = "sendtoreader";

        public static NamespaceManager CreateNamespaceManager()
        {
            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            return namespaceManager;
        }

        public static void Initialize()
        {
            // Using Http to be friendly with outbound firewalls
            ServiceBusEnvironment.SystemConnectivity.Mode = ConnectivityMode.Http;

            string connectionString = CloudConfigurationManager.GetSetting("Microsoft.ServiceBus.ConnectionString");

            // Create the namespace manager which gives you access to 
            // management operations
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            // Create the queue if it does not exist already
            if (!namespaceManager.QueueExists(QueueName))
            {
                namespaceManager.CreateQueue(QueueName);
            }

            // Get a client to the queue
            var messagingFactory = MessagingFactory.Create(
                namespaceManager.Address,
                namespaceManager.Settings.TokenProvider);
            sendtoreaderClient = messagingFactory.CreateQueueClient("sendtoreader");
        }
    }
}