using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using AGUI.Events;

namespace AGUI;

/// <summary>
/// AG-UI agent implementation that communicates with a remote AG-UI server over HTTP and parses SSE events
/// </summary>
public class HttpClientAGUIAgent : AGUIAgent
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;

    /// <summary>
    /// Initializes a new instance of HttpClientAGUIAgent
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests</param>
    /// <param name="endpoint">The AG-UI endpoint URL</param>
    /// <param name="name">Optional agent name</param>
    /// <param name="description">Optional agent description</param>
    public HttpClientAGUIAgent(
        HttpClient httpClient,
        string endpoint,
        string? name = null,
        string? description = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Runs the agent by sending the input to the remote server and streaming back AG-UI events
    /// </summary>
    public override async IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        {
            Content = JsonContent.Create(input, options: AGUIJsonSerializerContext.Default.Options)
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // Parse SSE stream using System.Net.ServerSentEvents
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        
        // Create SSE parser with custom item parser that deserializes AG-UI events
        var parser = SseParser.Create(stream, ParseAGUIEvent);

        await foreach (var sseItem in parser.EnumerateAsync(cancellationToken))
        {
            if (sseItem.Data != null)
            {
                yield return sseItem.Data;
            }
        }
    }

    private static BaseEvent? ParseAGUIEvent(string eventType, ReadOnlySpan<byte> data)
    {
        try
        {
            // Use polymorphic deserialization configured via JsonPolymorphic attribute on BaseEvent
            return System.Text.Json.JsonSerializer.Deserialize<BaseEvent>(
                data, AGUIJsonSerializerContext.Default.BaseEvent);
        }
        catch
        {
            // Ignore malformed events
            return null;
        }
    }
}
