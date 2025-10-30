using System.Text.Json.Serialization;

namespace MicrosoftAgentFrameworkServer.Models;

public static class EventTypeNames
{
    public const string RunStarted = "RUN_STARTED";
    public const string RunFinished = "RUN_FINISHED";
    public const string TextMessageStart = "TEXT_MESSAGE_START";
    public const string TextMessageContent = "TEXT_MESSAGE_CONTENT";
    public const string TextMessageEnd = "TEXT_MESSAGE_END";
}

[JsonPolymorphic(IgnoreUnrecognizedTypeDiscriminators = true, UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(RunStartedEvent), EventTypeNames.RunStarted)]
[JsonDerivedType(typeof(RunFinishedEvent), EventTypeNames.RunFinished)]
[JsonDerivedType(typeof(TextMessageStartEvent), EventTypeNames.TextMessageStart)]
[JsonDerivedType(typeof(TextMessageContentEvent), EventTypeNames.TextMessageContent)]
[JsonDerivedType(typeof(TextMessageEndEvent), EventTypeNames.TextMessageEnd)]
public abstract class BaseEvent
{
    [JsonPropertyName("type")]
    public string Type { get; }

    protected BaseEvent(string type) => Type = type;
}

public sealed class RunStartedEvent : BaseEvent
{
    public RunStartedEvent() : base(EventTypeNames.RunStarted) { }

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; }

    [JsonPropertyName("runId")]
    public string RunId { get; set; }
}

public sealed class RunFinishedEvent : BaseEvent
{
    public RunFinishedEvent() : base(EventTypeNames.RunFinished) { }

    [JsonPropertyName("threadId")]
    public string ThreadId { get; set; }

    [JsonPropertyName("runId")]
    public string RunId { get; set; }
}

public sealed class TextMessageStartEvent : BaseEvent
{
    public TextMessageStartEvent() : base(EventTypeNames.TextMessageStart) { }

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; }
}

public sealed class TextMessageContentEvent : BaseEvent
{
    public TextMessageContentEvent() : base(EventTypeNames.TextMessageContent) { }

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }

    [JsonPropertyName("delta")]
    public string Delta { get; set; }
}

public sealed class TextMessageEndEvent : BaseEvent
{
    public TextMessageEndEvent() : base(EventTypeNames.TextMessageEnd) { }

    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }
}
