using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Azure.Identity;

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

        var cosmosDbEndpoint = Environment.GetEnvironmentVariable("CosmosDBEndpoint");
        CosmosClient cosmosClient = new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(), options);
        services.AddSingleton(cosmosClient);
    })
    .Build();

host.Run();
