using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Message containing the result of a tool call
/// </summary>
public class ToolMessage : Message
{
    /// <summary>
    /// Role is always Tool for ToolMessage
    /// </summary>
    [JsonIgnore]
    public override Role Role => Role.Tool;

    /// <summary>
    /// Content of the tool result
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// ID of the tool call this is responding to
    /// </summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>
    /// Optional error information if the tool call failed
    /// </summary>
    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }
}
