using System.Text.Json.Serialization;

namespace AGUI.Events;

public class RunFinishedEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.RUN_FINISHED;

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; } = string.Empty;

    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; set; }
}
