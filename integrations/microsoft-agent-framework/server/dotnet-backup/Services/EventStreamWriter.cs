using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using MicrosoftAgentFrameworkServer.Models;

namespace MicrosoftAgentFrameworkServer.Services;

internal static class EventStreamWriter
{
    private const string ProtoMediaType = "application/vnd.ag-ui.event+proto";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static JsonSerializerOptions JsonOptions => SerializerOptions;

    public static bool AcceptsProtobuf(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Accept", out var headerValues))
        {
            return false;
        }

        var parsedValues = new List<MediaTypeHeaderValue>();

        foreach (var headerValue in headerValues)
        {
            if (string.IsNullOrWhiteSpace(headerValue))
            {
                continue;
            }

            if (MediaTypeHeaderValue.TryParse(headerValue, out var mediaType))
            {
                parsedValues.Add(mediaType);
            }
        }

        return parsedValues.Any(value => string.Equals(value.MediaType.Value, ProtoMediaType, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task WriteSseAsync(HttpContext context, IEnumerable<BaseEvent> events, CancellationToken cancellationToken)
    {
        var response = context.Response;

        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";
        response.Headers.Pragma = "no-cache";
        response.Headers["Content-Type"] = "text/event-stream";

        await response.StartAsync(cancellationToken);

        foreach (var @event in events)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = JsonSerializer.Serialize(@event, SerializerOptions);
            await response.WriteAsync($"data: {payload}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }

        await response.Body.FlushAsync(cancellationToken);
    }

}
