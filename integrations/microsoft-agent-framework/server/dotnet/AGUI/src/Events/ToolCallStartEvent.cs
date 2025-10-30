using System.Text.Json.Serialization;

namespace AGUI.Events;

/// <summary>
/// Event emitted when a tool call starts
/// </summary>
public class ToolCallStartEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TOOL_CALL_START;

    /// <summary>
    /// Unique identifier for this tool call
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; set; }

    /// <summary>
    /// Name of the tool being called
    /// </summary>
    [JsonPropertyName("toolCallName")]
    public required string ToolCallName { get; set; }

    /// <summary>
    /// Optional parent message ID
    /// </summary>
    [JsonPropertyName("parentMessageId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ParentMessageId { get; set; }
}
