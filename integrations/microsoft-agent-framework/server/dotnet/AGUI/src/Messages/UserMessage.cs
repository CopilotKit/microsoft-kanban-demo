using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Message from the user
/// </summary>
public class UserMessage : Message
{
    /// <summary>
    /// Role is always User for UserMessage
    /// </summary>
    [JsonIgnore]
    public override Role Role => Role.User;

    /// <summary>
    /// Content of the user message
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
