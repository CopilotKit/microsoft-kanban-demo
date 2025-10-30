using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// System message (alternative to developer message)
/// </summary>
public class SystemMessage : Message
{
    /// <summary>
    /// Role is always System for SystemMessage
    /// </summary>
    [JsonIgnore]
    public override Role Role => Role.System;

    /// <summary>
    /// Content of the system message
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
