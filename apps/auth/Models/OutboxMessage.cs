using System.Text.Json;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public DateTimeOffset CreatedAt { get; set; }
}
