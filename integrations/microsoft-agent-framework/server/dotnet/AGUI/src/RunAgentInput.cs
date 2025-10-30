using System.Text.Json;
using System.Text.Json.Serialization;
using AGUI.Messages;

namespace AGUI;

/// <summary>
/// Input for running an agent according to AG-UI protocol
/// </summary>
public class RunAgentInput
{
    /// <summary>
    /// Unique identifier for the conversation thread
    /// </summary>
    [JsonPropertyName("threadId")]
    public required string ThreadId { get; set; }

    /// <summary>
    /// Unique identifier for this agent run
    /// </summary>
    [JsonPropertyName("runId")]
    public required string RunId { get; set; }

    /// <summary>
    /// Agent state (arbitrary JSON-serializable object)
    /// </summary>
    [JsonPropertyName("state")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? State { get; set; }

    /// <summary>
    /// Messages in the conversation
    /// </summary>
    [JsonPropertyName("messages")]
    public Message[] Messages { get; init; } = [];

    /// <summary>
    /// Tools available to the agent
    /// </summary>
    [JsonPropertyName("tools")]
    public AGUITool[] Tools { get; init; } = [];

    /// <summary>
    /// Additional context for the agent
    /// </summary>
    [JsonPropertyName("context")]
    public Context[] ContextItems { get; init; } = [];

    /// <summary>
    /// Properties forwarded from the client
    /// </summary>
    [JsonPropertyName("forwardedProps")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ForwardedProps { get; init; }
}
