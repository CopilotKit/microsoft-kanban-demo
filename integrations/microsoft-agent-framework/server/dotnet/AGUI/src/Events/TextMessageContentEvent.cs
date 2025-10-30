using System.Text.Json.Serialization;

namespace AGUI.Events;

public class TextMessageContentEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TEXT_MESSAGE_CONTENT;

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("delta")]
    public string Delta { get; set; } = string.Empty;
}
