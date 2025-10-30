using AGUI.Events;

namespace AGUI;

/// <summary>
/// Helper class for building and streaming tool call events
/// </summary>
public class ToolCallStreamBuilder
{
    private readonly string _toolCallId;
    private readonly string _toolCallName;
    private readonly string? _parentMessageId;

    /// <summary>
    /// Creates a new tool call stream builder
    /// </summary>
    /// <param name="toolCallId">Unique identifier for the tool call</param>
    /// <param name="toolCallName">Name of the tool being called</param>
    /// <param name="parentMessageId">Optional parent message ID</param>
    public ToolCallStreamBuilder(string toolCallId, string toolCallName, string? parentMessageId = null)
    {
        _toolCallId = toolCallId;
        _toolCallName = toolCallName;
        _parentMessageId = parentMessageId;
    }

    /// <summary>
    /// Creates the start event for the tool call
    /// </summary>
    public ToolCallStartEvent CreateStartEvent()
    {
        return new ToolCallStartEvent
        {
            ToolCallId = _toolCallId,
            ToolCallName = _toolCallName,
            ParentMessageId = _parentMessageId
        };
    }

    /// <summary>
    /// Creates an arguments delta event
    /// </summary>
    /// <param name="delta">JSON fragment to stream</param>
    public ToolCallArgsEvent CreateArgsEvent(string delta)
    {
        return new ToolCallArgsEvent
        {
            ToolCallId = _toolCallId,
            Delta = delta
        };
    }

    /// <summary>
    /// Creates the end event for the tool call
    /// </summary>
    public ToolCallEndEvent CreateEndEvent()
    {
        return new ToolCallEndEvent
        {
            ToolCallId = _toolCallId
        };
    }

    /// <summary>
    /// Streams a complete JSON string as multiple delta events
    /// </summary>
    /// <param name="jsonArguments">Complete JSON arguments string</param>
    /// <param name="chunkSize">Size of each delta chunk (default 100 characters)</param>
    /// <returns>Enumerable of args events</returns>
    public IEnumerable<ToolCallArgsEvent> StreamArguments(string jsonArguments, int chunkSize = 100)
    {
        if (string.IsNullOrEmpty(jsonArguments))
        {
            yield break;
        }

        for (int i = 0; i < jsonArguments.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, jsonArguments.Length - i);
            var delta = jsonArguments.Substring(i, length);
            yield return CreateArgsEvent(delta);
        }
    }

    /// <summary>
    /// Creates a complete sequence of events for a tool call
    /// </summary>
    /// <param name="jsonArguments">Complete JSON arguments string</param>
    /// <param name="chunkSize">Size of each delta chunk</param>
    /// <returns>Enumerable of all events (start, args deltas, end)</returns>
    public IEnumerable<BaseEvent> CreateCompleteSequence(string jsonArguments, int chunkSize = 100)
    {
        yield return CreateStartEvent();
        
        foreach (var argsEvent in StreamArguments(jsonArguments, chunkSize))
        {
            yield return argsEvent;
        }
        
        yield return CreateEndEvent();
    }
}
