using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using Azure.Messaging.ServiceBus;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var options = new CosmosClientOptions()
        {
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        };            

        // Add the CosmosClient to the services collection
        var cosmosDbEndpoint = Environment.GetEnvironmentVariable("CosmosDBEndpoint");
        CosmosClient cosmosClient = new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(), options);
        services.AddSingleton(cosmosClient);

        // Add a ServiceBusClient to the services collection
        var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusNamespace");
        var serviceBusClient = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
        services.AddSingleton(serviceBusClient);

        // Add the ServiceBusSender for a specific topic to the services collection
        var topicName = Environment.GetEnvironmentVariable("ServiceBusTopicName");
        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<ServiceBusClient>().CreateSender(topicName));
    })
    .Build();

host.Run();
