using System.Text.Json.Serialization;

namespace AGUI.Events;

public class RunStartedEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.RUN_STARTED;

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;
}
