using System.Text.Json;
using System.Text.Json.Serialization;

namespace MicrosoftAgentFrameworkServer.Models;

public sealed class RunAgentInput
{
    public string? ThreadId { get; set; }

    public string? RunId { get; set; }

    public List<Tool>? Tools { get; set; }

    public List<JsonElement>? Context { get; set; }

    public ForwardedProps? ForwardedProps { get; set; }

    public Dictionary<string, JsonElement>? State { get; set; }

    public List<Message>? Messages { get; set; }

    // The payload can contain additional properties that the sample server does not use.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class Tool
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public JsonElement? Parameters { get; set; }
}

public sealed class ForwardedProps
{
    public JsonElement? Config { get; set; }

    public JsonElement? ThreadMetadata { get; set; }
}

public sealed class Message
{
    public string? Id { get; set; }

    public string? Role { get; set; }

    public string? Content { get; set; }
}
