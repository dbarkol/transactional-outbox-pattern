using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using System.Text.Json;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text;

namespace Sprockets
{
    public class OutboxProcessor
    {
        private readonly ILogger _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;     
        private readonly ServiceBusSender _serviceBusSender;   

        public OutboxProcessor(ILoggerFactory loggerFactory, CosmosClient cosmosClient, ServiceBusSender serviceBusSender)
        {
            _logger = loggerFactory.CreateLogger<OutboxProcessor>();
            _cosmosClient = cosmosClient;
            _serviceBusSender = serviceBusSender;

            // Get the database and container names
            var databaseId = Environment.GetEnvironmentVariable("CosmosDBDatabaseId");
            var containerId = Environment.GetEnvironmentVariable("CosmosDBContainerId");            
            
            // Get the container
            _container = _cosmosClient.GetContainer(databaseId, containerId);            
        }

        /// <summary>
        /// This function is triggered by changes to the Cosmos DB container.
        /// 
        /// The function processes the outbox orders and sends the order to 
        /// the Service Bus topic.
        /// 
        /// A delayed retry policy is applied to the function to ensure that
        /// the function is retried in case of a failure. The retry policy
        /// also allows ensures that change feed events are not lost.
        /// </summary>
        [Function("OutboxProcessor")]
        [FixedDelayRetry(5, "00:00:10")]
        public async Task Run(
            [CosmosDBTrigger(
                databaseName: "%CosmosDBDatabaseId%",
                containerName: "%CosmosDBContainerId%",
                Connection = "CosmosDBConnection",
                CreateLeaseContainerIfNotExists = true,
                LeaseContainerName ="%LeaseContainerName%")] IReadOnlyList<JsonElement> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Documents modified: " + input.Count);

                // Iterate over each document and handle the ones that
                // are for the outbox orders and have not been processed
                foreach (var document in input)
                {
                    // The orderProcessed property will identify if the document
                    // is an outbox order that has not been processed
                    if (document.TryGetProperty("orderProcessed", out var orderProcessedProperty))
                    {
                        if (!orderProcessedProperty.GetBoolean())
                        {
                            // Get the id properties from the document so they
                            // can be used to retrieve the items from the container
                            _logger.LogInformation("OrderProcessed is false");
                            var orderId = document.GetProperty("orderId").GetString();
                            var id = document.GetProperty("id").GetString();
                            var partitionKey = new PartitionKey(orderId);

                            // Retrieve the order object to send to the Service Bus topic                            
                            var order = await _container.ReadItemAsync<Order>(orderId, partitionKey);

                            // Initialize the Service Bus message 
                            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order.Resource)));
                            message.ContentType = "application/json";
                            message.ApplicationProperties.Add("MessageType", "OrderCreated");

                            // Set the MessageID to the order ID 
                            // for deduplication purposes
                            message.MessageId = order.Resource.OrderId;
                            
                            // Send the message to the Service Bus topic
                            await _serviceBusSender.SendMessageAsync(message);

                            // Get the document and update the processed state to true
                            var orderOutbox = await _container.ReadItemAsync<OrderOutbox>(id, partitionKey);
                            orderOutbox.Resource.OrderProcessed = true;
                            await _container.ReplaceItemAsync(orderOutbox.Resource, id, partitionKey);
                        }
                    }
                }

            }
        }
    }
}
