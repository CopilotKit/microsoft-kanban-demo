using System.Text.Json;
using System.Text.Json.Serialization;

namespace AGUI.Events;

public class BaseEventJsonConverter : JsonConverter<BaseEvent>
{
    private const string TypeDiscriminatorPropertyName = "type";

    public override BaseEvent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize to JsonElement first to inspect properties without recursion
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        // Try to get the discriminator property
        if (!jsonElement.TryGetProperty(TypeDiscriminatorPropertyName, out var discriminatorElement))
        {
            throw new JsonException($"Missing required property '{TypeDiscriminatorPropertyName}' for BaseEvent deserialization");
        }

        var discriminator = discriminatorElement.GetString();
        
        // Map discriminator to concrete type
        Type concreteType = discriminator switch
        {
            EventType.RUN_STARTED => typeof(RunStartedEvent),
            EventType.RUN_FINISHED => typeof(RunFinishedEvent),
            EventType.RUN_ERROR => typeof(RunErrorEvent),
            EventType.TEXT_MESSAGE_START => typeof(TextMessageStartEvent),
            EventType.TEXT_MESSAGE_CONTENT => typeof(TextMessageContentEvent),
            EventType.TEXT_MESSAGE_END => typeof(TextMessageEndEvent),
            EventType.TOOL_CALL_START => typeof(ToolCallStartEvent),
            EventType.TOOL_CALL_ARGS => typeof(ToolCallArgsEvent),
            EventType.TOOL_CALL_END => typeof(ToolCallEndEvent),
            EventType.TOOL_CALL_RESULT => typeof(ToolCallResultEvent),
            EventType.STATE_SNAPSHOT => typeof(StateSnapshotEvent),
            _ => throw new JsonException($"Unknown event type: {discriminator}")
        };

        // Deserialize using JsonElement.Deserialize which respects the type directly
        return (BaseEvent?)jsonElement.Deserialize(concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, BaseEvent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        // Write the discriminator property first
        writer.WriteString(TypeDiscriminatorPropertyName, value.Type);
        
        // Write other properties by serializing to JsonElement first
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        foreach (var property in element.EnumerateObject())
        {
            // Skip the type property as we already wrote it
            if (!string.Equals(property.Name, TypeDiscriminatorPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                property.WriteTo(writer);
            }
        }
        
        writer.WriteEndObject();
    }
}
