using System.Text.Json.Serialization;

namespace AGUI.Events;

/// <summary>
/// Event emitted when a tool call completes
/// </summary>
public class ToolCallEndEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TOOL_CALL_END;

    /// <summary>
    /// The tool call ID that has completed
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; set; }
}
