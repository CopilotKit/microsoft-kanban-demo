// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AGUI;
using AGUI.Events;

namespace Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

/// <summary>
/// Represents a result that writes a stream of AG-UI events as server-sent events to the response.
/// </summary>
public sealed class AGUIServerSentEventsResult : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    private readonly IAsyncEnumerable<BaseEvent> _events;

    /// <inheritdoc/>
    public int? StatusCode => StatusCodes.Status200OK;

    /// <summary>
    /// Initializes a new instance of the <see cref="AGUIServerSentEventsResult"/> class.
    /// </summary>
    /// <param name="events">The stream of AG-UI events to write.</param>
    public AGUIServerSentEventsResult(IAsyncEnumerable<BaseEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        _events = events;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Configure SSE response headers
        httpContext.Response.ContentType = "text/event-stream; charset=utf-8";
        httpContext.Response.Headers.CacheControl = "no-cache,no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.ContentEncoding = "identity";

        // Disable response buffering for SSE
        var bufferingFeature = httpContext.Features.GetRequiredFeature<IHttpResponseBodyFeature>();
        bufferingFeature.DisableBuffering();

        // Write SSE events
        var writer = new StreamWriter(httpContext.Response.Body, Encoding.UTF8, leaveOpen: true);
        
        try
        {
            await foreach (var aguiEvent in _events.WithCancellation(httpContext.RequestAborted))
            {
                // Serialize the event to JSON using BaseEvent type to include polymorphic discriminator
                var json = JsonSerializer.Serialize<BaseEvent>(aguiEvent, AGUIJsonSerializerContext.Default.BaseEvent);

                // Write SSE format: "data: {json}\n\n"
                await writer.WriteAsync("data: ");
                await writer.WriteAsync(json);
                await writer.WriteAsync("\n\n");
                await writer.FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - this is expected for SSE connections
        }
    }

    /// <inheritdoc />
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(
            StatusCodes.Status200OK,
            typeof(BaseEvent),
            contentTypes: ["text/event-stream"]));
    }
}
