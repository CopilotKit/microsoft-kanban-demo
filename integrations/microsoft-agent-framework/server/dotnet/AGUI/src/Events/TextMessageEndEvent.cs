using System.Text.Json.Serialization;

namespace AGUI.Events;

public class TextMessageEndEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TEXT_MESSAGE_END;

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;
}
