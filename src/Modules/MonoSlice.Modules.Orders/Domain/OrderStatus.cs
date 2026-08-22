using System.Text.Json.Serialization;

namespace MonoSlice.Modules.Orders.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    Paid,
    Expired,
    Failed
}
