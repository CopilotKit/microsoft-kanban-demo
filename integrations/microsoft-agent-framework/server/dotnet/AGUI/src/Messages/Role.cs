using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace AGUI.Messages;

/// <summary>
/// Message role as defined by AG-UI protocol
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Role>))]
public enum Role
{
    [EnumMember(Value = "developer")]
    Developer,

    [EnumMember(Value = "system")]
    System,

    [EnumMember(Value = "assistant")]
    Assistant,

    [EnumMember(Value = "user")]
    User,

    [EnumMember(Value = "tool")]
    Tool
}
