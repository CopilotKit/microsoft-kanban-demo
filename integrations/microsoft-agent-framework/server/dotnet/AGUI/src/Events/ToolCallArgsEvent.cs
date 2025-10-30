using System.Text.Json.Serialization;

namespace AGUI.Events;

/// <summary>
/// Event emitted to stream tool call arguments incrementally
/// </summary>
public class ToolCallArgsEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TOOL_CALL_ARGS;

    /// <summary>
    /// The tool call ID this delta belongs to
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; set; }

    /// <summary>
    /// Delta (incremental fragment) of the JSON arguments
    /// </summary>
    [JsonPropertyName("delta")]
    public required string Delta { get; set; }
}
