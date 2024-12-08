using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Cosmos;

namespace src
{
    public class OrderMaker
    {
        private readonly ILogger<OrderMaker> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public OrderMaker(ILogger<OrderMaker> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;

            // Get the database and container names
            var databaseId = Environment.GetEnvironmentVariable("CosmosDBDatabaseId");
            var containerId = Environment.GetEnvironmentVariable("CosmosDBContainerId");            
            
            // Get the container
            _container = _cosmosClient.GetContainer(databaseId, containerId);
        }

        [Function("OrderMaker")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("OrderMaker invoked");

            var incomingOrder = await req.ReadFromJsonAsync<Order>();
            if (incomingOrder == null)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            incomingOrder.Id = incomingOrder.OrderId;
            var orderOutbox = new OrderOutbox
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = incomingOrder.OrderId,
                Quantity = incomingOrder.Quantity,
                AccountNumber = incomingOrder.AccountNumber,
                OrderProcessed = false
            };

            PartitionKey partitionKey = new PartitionKey(incomingOrder.OrderId);

            var batch = _container.CreateTransactionalBatch(partitionKey)
                .CreateItem<Order>(incomingOrder)
                .CreateItem<OrderOutbox>(orderOutbox);
            

            using TransactionalBatchResponse batchResponse = await batch.ExecuteAsync();   
            if (batchResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Transactional batch succeeded");
                for (var i = 0; i < batchResponse.Count; i++)
                {
                    var result = batchResponse.GetOperationResultAtIndex<dynamic>(i);
                    Console.WriteLine($"Document {i + 1}:");
                    Console.WriteLine(result.Resource);
                }
            }
            else
            {
                Console.WriteLine("Transactional batch failed");
                for (var i = 0; i < batchResponse.Count; i++)
                {
                    var result = batchResponse.GetOperationResultAtIndex<dynamic>(i);
                    Console.WriteLine($"Document {i + 1}: {result.StatusCode}");
                }
            }


            return req.CreateResponse(HttpStatusCode.OK);
        }
    }
}
