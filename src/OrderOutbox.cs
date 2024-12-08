using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class OrderOutbox
{

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty(PropertyName = "orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonProperty(PropertyName = "quantity")]
    public int Quantity { get; set; }

    [JsonProperty(PropertyName = "accountNumber")]
    public int AccountNumber { get; set; }

    [JsonProperty(PropertyName = "orderProcessed")]
    public bool OrderProcessed { get; set; }
}
