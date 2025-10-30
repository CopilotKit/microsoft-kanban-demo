using System.Text.Json.Serialization;

namespace AGUI.Events;

public class RunErrorEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.RUN_ERROR;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Code { get; set; }
}
