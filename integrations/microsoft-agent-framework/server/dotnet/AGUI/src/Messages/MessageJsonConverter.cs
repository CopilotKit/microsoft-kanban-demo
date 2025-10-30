using System.Text.Json;
using System.Text.Json.Serialization;

namespace AGUI.Messages;

public class MessageJsonConverter : JsonConverter<Message>
{
    private const string TypeDiscriminatorPropertyName = "role";

    public override Message? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize to JsonElement first to inspect properties without recursion
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(ref reader, options);

        // Try to get the discriminator property
        if (!jsonElement.TryGetProperty(TypeDiscriminatorPropertyName, out var discriminatorElement))
        {
            throw new JsonException($"Missing required property '{TypeDiscriminatorPropertyName}' for Message deserialization");
        }

        var discriminator = discriminatorElement.GetString();
        
        // Map discriminator to concrete type
        Type concreteType = discriminator switch
        {
            "developer" => typeof(DeveloperMessage),
            "system" => typeof(SystemMessage),
            "assistant" => typeof(AssistantMessage),
            "user" => typeof(UserMessage),
            "tool" => typeof(ToolMessage),
            _ => throw new JsonException($"Unknown message role: {discriminator}")
        };

        // Deserialize using JsonElement.Deserialize which respects the type directly
        return (Message?)jsonElement.Deserialize(concreteType, options);
    }

    public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
    {
        // Get the TypeInfo for the concrete type to serialize properly
        var typeInfo = options.GetTypeInfo(value.GetType());
        
        writer.WriteStartObject();
        
        // Write the discriminator property first
        writer.WriteString(TypeDiscriminatorPropertyName, value.Role.ToString().ToLowerInvariant());
        
        // Write other properties by serializing to JsonElement first
        var element = JsonSerializer.SerializeToElement(value, value.GetType(), options);
        foreach (var property in element.EnumerateObject())
        {
            // Skip the role property as we already wrote it
            if (!string.Equals(property.Name, TypeDiscriminatorPropertyName, StringComparison.OrdinalIgnoreCase))
            {
                property.WriteTo(writer);
            }
        }
        
        writer.WriteEndObject();
    }
}
