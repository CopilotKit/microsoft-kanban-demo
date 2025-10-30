using System.Text.Json.Serialization;

namespace AGUI.Events;

public class TextMessageStartEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TEXT_MESSAGE_START;

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}
