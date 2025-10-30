using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Message from the developer (system prompt, instructions)
/// </summary>
public class DeveloperMessage : Message
{
    /// <summary>
    /// Role is always Developer for DeveloperMessage
    /// </summary>
    [JsonIgnore]
    public override Role Role => Role.Developer;

    /// <summary>
    /// Content of the developer message
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
