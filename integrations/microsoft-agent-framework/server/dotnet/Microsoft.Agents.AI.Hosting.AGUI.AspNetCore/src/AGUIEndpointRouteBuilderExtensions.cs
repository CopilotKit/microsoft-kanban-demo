using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using AGUI;
using System.Text.Json;

namespace Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;

public static class AGUIEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an AG-UI agent endpoint that streams AG-UI events
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <param name="pattern">The URL pattern</param>
    /// <param name="agentFactory">Factory function to create an AGUIAgent from the input and HTTP context</param>
    /// <returns>The route handler builder for further configuration</returns>
    public static RouteHandlerBuilder MapAGUIAgent(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<RunAgentInput, HttpContext, AGUIAgent> agentFactory)
    {
        return endpoints.MapPost(pattern, async (RunAgentInput input, HttpContext context, CancellationToken cancellationToken) =>
        {
            try
            {
                // Get AGUIAgent from factory
                var agent = agentFactory(input, context);

                // Run the agent and get the event stream
                var eventStream = agent.RunAsync(input, cancellationToken);

                // Return ServerSentEventsResult for proper SSE handling
                return new AGUIServerSentEventsResult(eventStream);
            }
            catch (JsonException ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid JSON", details = ex.Message }, cancellationToken: cancellationToken);
                return Results.Empty;
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken: cancellationToken);
                return Results.Empty;
            }
        });
    }

    /// <summary>
    /// Maps an AG-UI agent endpoint with a simple AGUIAgent instance
    /// </summary>
    /// <param name="endpoints">The endpoint route builder</param>
    /// <param name="pattern">The URL pattern</param>
    /// <param name="agent">The AGUIAgent instance to use</param>
    /// <returns>The route handler builder for further configuration</returns>
    public static RouteHandlerBuilder MapAGUIAgent(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        AGUIAgent agent)
    {
        return endpoints.MapAGUIAgent(pattern, (_, _) => agent);
    }
}
