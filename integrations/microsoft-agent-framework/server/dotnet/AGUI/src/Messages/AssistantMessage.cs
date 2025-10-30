using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Message from the assistant (agent)
/// </summary>
public class AssistantMessage : Message
{
    /// <summary>
    /// Role is always Assistant for AssistantMessage
    /// </summary>
    [JsonIgnore]
    public override Role Role => Role.Assistant;

    /// <summary>
    /// Content of the assistant message (optional for assistant messages with tool calls)
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }

    /// <summary>
    /// Tool calls made by the assistant
    /// </summary>
    [JsonPropertyName("toolCalls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolCall[]? ToolCalls { get; init; }
}
