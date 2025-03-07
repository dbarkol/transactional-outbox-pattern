# Transactional Outbox Pattern with Azure Functions

This repository provides a sample implementation of the Transactional Outbox Pattern using [Azure Functions](https://learn.microsoft.com/azure/azure-functions/functions-overview?pivots) and [Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/introduction). The pattern ensures reliable, event-driven communication between services by atomically persisting domain changes and associated events in a single transaction. 

This implementation leverages Cosmos DB's transactional capabilities for event storage and Azure Functions to asynchronously process and publish the events, enabling seamless integration across distributed systems. The sample is designed to be modular and scalable, serving as a foundation for building resilient and consistent microservices.

## Architecture

![Transactional Outbox Pattern with Azure Functions and Cosmos DB](https://madeofstrings.com/wp-content/uploads/2022/10/outbox-pattern-azure.png)

The overall flow can be broken down into these steps:

1. Incoming orders are sent from a client application to an HTTP-triggered Azure Function.

2. The Azure Function will save both the order details as well as any events that should be published to a message broker to Cosmos DB using a single transaction.

3. Another Azure Function, the outbox processor, is invoked from the Cosmos DB change feed when the transaction completes.

4. Events waiting to be published are retrieved from Cosmos DB.

5. If there are any pending events, the function will attempt to publish them to a [Service Bus topic](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-queues-topics-subscriptions).

6. After publishing to a message borker, the function will update the corresponding entries in Cosmos to a “Processed” state.