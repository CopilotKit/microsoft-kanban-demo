using AGUI.Events;

namespace AGUI;

/// <summary>
/// Base abstraction for AG-UI agents that can process requests and stream AG-UI events
/// </summary>
/// <remarks>
/// <para>
/// Agents process user messages and generate responses through a stream of AG-UI events.
/// The event stream can include text messages, tool calls, state updates, and metadata.
/// </para>
/// <para>
/// <strong>Tool Call Support:</strong> Agents can emit tool call events to request the frontend
/// to execute specific actions. Tool calls follow a three-phase lifecycle:
/// <list type="number">
/// <item><description><c>TOOL_CALL_START</c> - Indicates the beginning of a tool call with a unique ID and tool name</description></item>
/// <item><description><c>TOOL_CALL_ARGS</c> - Streams the JSON arguments incrementally as delta fragments</description></item>
/// <item><description><c>TOOL_CALL_END</c> - Marks the completion of the tool call</description></item>
/// </list>
/// Use the <see cref="ToolCallStreamBuilder"/> helper class to simplify tool call event generation.
/// </para>
/// <para>
/// The frontend executes the tool and returns results via <see cref="Messages.ToolMessage"/> objects,
/// which should be added to the message history for subsequent agent runs.
/// </para>
/// </remarks>
public abstract class AGUIAgent
{
    /// <summary>
    /// Gets the name of the agent
    /// </summary>
    public string? Name { get; protected set; }

    /// <summary>
    /// Gets the description of the agent
    /// </summary>
    public string? Description { get; protected set; }

    /// <summary>
    /// Runs the agent with the specified request and streams AG-UI events
    /// </summary>
    /// <param name="input">The AG-UI run input containing messages, tools, state, and context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of AG-UI events including text messages, tool calls, and state updates</returns>
    /// <remarks>
    /// The input contains a <see cref="RunAgentInput.Tools"/> array with available tool definitions.
    /// Implementations should pass these tools to their underlying LLM provider and emit tool call
    /// events when the model decides to use a tool.
    /// </remarks>
    public abstract IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        CancellationToken cancellationToken = default);
}
