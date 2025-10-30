using System.Text.Json.Serialization;

namespace AGUI.Events;

[JsonConverter(typeof(BaseEventJsonConverter))]
public abstract class BaseEvent
{
    [JsonIgnore]
    public abstract string Type { get; }

    [JsonPropertyName("timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Timestamp { get; set; }

    [JsonPropertyName("rawEvent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? RawEvent { get; init; }
}
