using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Function call within a tool call
/// </summary>
public class FunctionCall
{
    /// <summary>
    /// Name of the function being called
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Arguments for the function as JSON string
    /// </summary>
    [JsonPropertyName("arguments")]
    public required string Arguments { get; init; }
}

/// <summary>
/// Represents a tool call made by the assistant
/// </summary>
public class ToolCall
{
    /// <summary>
    /// Unique identifier for the tool call
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Type of tool call (always "function" for now)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    /// <summary>
    /// Function being called
    /// </summary>
    [JsonPropertyName("function")]
    public required FunctionCall Function { get; init; }
}
