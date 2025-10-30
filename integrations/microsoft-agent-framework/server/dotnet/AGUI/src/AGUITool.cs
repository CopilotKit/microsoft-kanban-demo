using System.Text.Json;
using System.Text.Json.Serialization;

namespace AGUI;

/// <summary>
/// Represents a tool that can be called by the agent
/// </summary>
public class AGUITool
{
    /// <summary>
    /// Name of the tool
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of what the tool does
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// JSON Schema defining the tool's parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    public JsonElement Parameters { get; init; }
}

/// <summary>
/// Provides additional context to the agent
/// </summary>
public class Context
{
    /// <summary>
    /// Description of the context
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// The context value
    /// </summary>
    [JsonPropertyName("value")]
    public required object Value { get; init; }
}
