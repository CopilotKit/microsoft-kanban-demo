using System.Text.Json.Serialization;

namespace AGUI.Events;

/// <summary>
/// Event emitted when a tool call result is available
/// </summary>
public class ToolCallResultEvent : BaseEvent
{
    [JsonIgnore]
    public override string Type => EventType.TOOL_CALL_RESULT;

    /// <summary>
    /// ID of the conversation message this result belongs to
    /// </summary>
    [JsonPropertyName("messageId")]
    public required string MessageId { get; set; }

    /// <summary>
    /// The tool call ID that this result corresponds to
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; set; }

    /// <summary>
    /// The actual result/output content from the tool execution
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }

    /// <summary>
    /// Optional role identifier, typically "tool" for tool results
    /// </summary>
    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }
}
