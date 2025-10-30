using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Base class for all message types in AG-UI protocol
/// </summary>
[JsonConverter(typeof(MessageJsonConverter))]
public abstract class Message
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Role of the message sender
    /// </summary>
    [JsonPropertyName("role")]
    public abstract Role Role { get; }

    /// <summary>
    /// Optional name for the message sender
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }
}
