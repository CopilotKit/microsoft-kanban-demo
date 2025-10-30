using System.Text.Json;
using System.Text.Json.Serialization;
using AGUI.Events;
using AGUI.Messages;

namespace AGUI;

/// <summary>
/// JSON source generation context for AG-UI types
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(RunAgentInput))]
[JsonSerializable(typeof(BaseEvent))]
[JsonSerializable(typeof(RunStartedEvent))]
[JsonSerializable(typeof(RunFinishedEvent))]
[JsonSerializable(typeof(RunErrorEvent))]
[JsonSerializable(typeof(TextMessageStartEvent))]
[JsonSerializable(typeof(TextMessageContentEvent))]
[JsonSerializable(typeof(TextMessageEndEvent))]
[JsonSerializable(typeof(ToolCallStartEvent))]
[JsonSerializable(typeof(ToolCallArgsEvent))]
[JsonSerializable(typeof(ToolCallEndEvent))]
[JsonSerializable(typeof(ToolCallResultEvent))]
[JsonSerializable(typeof(StateSnapshotEvent))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(DeveloperMessage))]
[JsonSerializable(typeof(SystemMessage))]
[JsonSerializable(typeof(UserMessage))]
[JsonSerializable(typeof(AssistantMessage))]
[JsonSerializable(typeof(ToolMessage))]
[JsonSerializable(typeof(AGUITool))]
[JsonSerializable(typeof(Context))]
[JsonSerializable(typeof(ToolCall))]
[JsonSerializable(typeof(FunctionCall))]
[JsonSerializable(typeof(Role))]
public partial class AGUIJsonSerializerContext : JsonSerializerContext
{
}
