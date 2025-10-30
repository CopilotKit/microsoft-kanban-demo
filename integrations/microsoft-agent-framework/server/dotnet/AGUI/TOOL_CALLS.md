# Tool Call Implementation Guide

## Overview

This guide shows how to implement tool calls in AG-UI .NET agents. Tool calls allow agents to request frontend execution of specific actions and receive results back.

## Tool Call Lifecycle

Every tool call follows a three-phase event sequence:

1. **TOOL_CALL_START** - Begins the tool call with ID and name
2. **TOOL_CALL_ARGS** - Streams JSON arguments as delta fragments (one or more events)
3. **TOOL_CALL_END** - Marks completion of the tool call

## Using ToolCallStreamBuilder

The `ToolCallStreamBuilder` helper class simplifies tool call event generation:

```csharp
using AGUI;
using AGUI.Events;

// Create a builder for a specific tool call
var toolCallId = ToolCallValidator.GenerateToolCallId();
var builder = new ToolCallStreamBuilder(
    toolCallId: toolCallId,
    toolCallName: "change_background",
    parentMessageId: "msg-123" // optional
);

// Option 1: Stream individual events manually
yield return builder.CreateStartEvent();
yield return builder.CreateArgsEvent("{\"background\":");
yield return builder.CreateArgsEvent("\"linear-gradient(to right, #ff0000, #0000ff)\"");
yield return builder.CreateArgsEvent("}");
yield return builder.CreateEndEvent();

// Option 2: Stream a complete JSON string automatically
foreach (var evt in builder.CreateCompleteSequence("{\"background\":\"blue\"}"))
{
    yield return evt;
}

// Option 3: Control chunk size for streaming
var args = "{\"background\":\"linear-gradient(to right, #ff0000, #0000ff)\"}";
foreach (var evt in builder.CreateCompleteSequence(args, chunkSize: 50))
{
    yield return evt;
}
```

## Example Agent Implementation

Here's a complete example of an agent that uses tools:

```csharp
using AGUI;
using AGUI.Events;
using AGUI.Messages;
using System.Text.Json;

public class ToolAwareAgent : AGUIAgent
{
    public override async IAsyncEnumerable<BaseEvent> RunAsync(
        RunAgentInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Start the run
        yield return new RunStartedEvent();

        // Access available tools from input
        var availableTools = input.Tools;

        // Process messages and determine if a tool should be called
        var lastMessage = input.Messages.LastOrDefault();
        if (lastMessage is UserMessage userMsg)
        {
            // Example: Check if user wants to change background
            if (userMsg.Content.Contains("change", StringComparison.OrdinalIgnoreCase) &&
                userMsg.Content.Contains("background", StringComparison.OrdinalIgnoreCase))
            {
                // Validate tool exists
                if (ToolCallValidator.IsToolNameValid("change_background", availableTools))
                {
                    // Generate unique tool call ID
                    var toolCallId = ToolCallValidator.GenerateToolCallId();

                    // Create tool call arguments
                    var args = JsonSerializer.Serialize(new
                    {
                        background = "linear-gradient(135deg, #667eea 0%, #764ba2 100%)"
                    });

                    // Stream the tool call
                    var builder = new ToolCallStreamBuilder(
                        toolCallId,
                        "change_background"
                    );

                    foreach (var evt in builder.CreateCompleteSequence(args))
                    {
                        yield return evt;
                    }
                }
            }
            else
            {
                // Regular text response
                var messageId = $"msg_{Guid.NewGuid():N}";

                yield return new TextMessageStartEvent
                {
                    MessageId = messageId,
                    Role = "assistant"
                };

                yield return new TextMessageContentEvent
                {
                    MessageId = messageId,
                    Delta = "I can help with that!"
                };

                yield return new TextMessageEndEvent
                {
                    MessageId = messageId
                };
            }
        }

        // Finish the run
        yield return new RunFinishedEvent();
    }
}
```

## Handling Tool Results

When the frontend executes a tool, it returns a `ToolMessage` in the next agent run:

```csharp
// Check for tool messages in input
var toolMessages = input.Messages.OfType<ToolMessage>();

foreach (var toolMsg in toolMessages)
{
    if (toolMsg.Error != null)
    {
        // Tool execution failed
        Console.WriteLine($"Tool {toolMsg.ToolCallId} failed: {toolMsg.Error}");
    }
    else
    {
        // Tool executed successfully
        Console.WriteLine($"Tool {toolMsg.ToolCallId} result: {toolMsg.Content}");
    }
}
```

## Multiple Concurrent Tool Calls

You can emit multiple tool calls in the same agent run:

```csharp
var toolCall1Id = ToolCallValidator.GenerateToolCallId();
var toolCall2Id = ToolCallValidator.GenerateToolCallId();

var builder1 = new ToolCallStreamBuilder(toolCall1Id, "fetch_weather");
var builder2 = new ToolCallStreamBuilder(toolCall2Id, "fetch_news");

// Stream first tool call
foreach (var evt in builder1.CreateCompleteSequence("{\"location\":\"London\"}"))
{
    yield return evt;
}

// Stream second tool call
foreach (var evt in builder2.CreateCompleteSequence("{\"topic\":\"technology\"}"))
{
    yield return evt;
}
```

## Validation Best Practices

Always validate tool calls before emitting events:

```csharp
var toolName = "change_background";

// 1. Check tool exists
if (!ToolCallValidator.IsToolNameValid(toolName, input.Tools))
{
    throw new InvalidOperationException($"Tool '{toolName}' not available");
}

// 2. Generate unique ID
var toolCallId = ToolCallValidator.GenerateToolCallId();

// 3. Get tool definition
var tool = ToolCallValidator.GetToolByName(toolName, input.Tools);

// 4. Validate arguments (basic JSON validation)
var args = "{\"background\":\"blue\"}";
if (!ToolCallValidator.ValidateToolArguments(args, tool!))
{
    throw new ArgumentException("Invalid tool arguments");
}

// 5. Emit tool call events
var builder = new ToolCallStreamBuilder(toolCallId, toolName);
foreach (var evt in builder.CreateCompleteSequence(args))
{
    yield return evt;
}
```

## Integration with LLM Providers

When integrating with LLM providers like OpenAI or Anthropic, map AG-UI tools to their format:

```csharp
// Convert AG-UI tools to OpenAI function format
var openAITools = input.Tools.Select(t => new
{
    type = "function",
    function = new
    {
        name = t.Name,
        description = t.Description,
        parameters = JsonSerializer.Deserialize<object>(t.Parameters.GetRawText())
    }
}).ToArray();

// Pass to OpenAI API
// When OpenAI returns tool calls, emit AG-UI tool call events
```

## Error Handling

Handle tool call errors appropriately:

```csharp
try
{
    var toolCallId = ToolCallValidator.GenerateToolCallId();
    var builder = new ToolCallStreamBuilder(toolCallId, "risky_operation");
    
    // Attempt to emit tool call
    foreach (var evt in builder.CreateCompleteSequence("{\"param\":\"value\"}"))
    {
        yield return evt;
    }
}
catch (Exception ex)
{
    // Emit error event
    yield return new RunErrorEvent
    {
        Error = ex.Message
    };
}
```

## Testing Tool Calls

Test tool call implementations thoroughly:

```csharp
[Fact]
public async Task Agent_EmitsToolCallEvents_InCorrectOrder()
{
    var agent = new ToolAwareAgent();
    var input = new RunAgentInput
    {
        ThreadId = "test-thread",
        RunId = "test-run",
        Messages = new[]
        {
            new UserMessage
            {
                Id = "msg-1",
                Content = "Change the background to blue"
            }
        },
        Tools = new[]
        {
            new AGUITool
            {
                Name = "change_background",
                Description = "Changes the background color",
                Parameters = JsonDocument.Parse("{\"type\":\"object\",\"properties\":{\"background\":{\"type\":\"string\"}}}").RootElement
            }
        }
    };

    var events = new List<BaseEvent>();
    await foreach (var evt in agent.RunAsync(input))
    {
        events.Add(evt);
    }

    // Verify event sequence
    var toolEvents = events.Where(e => 
        e is ToolCallStartEvent || 
        e is ToolCallArgsEvent || 
        e is ToolCallEndEvent
    ).ToList();

    Assert.IsType<ToolCallStartEvent>(toolEvents[0]);
    Assert.IsType<ToolCallArgsEvent>(toolEvents[1]);
    Assert.IsType<ToolCallEndEvent>(toolEvents.Last());
}
```

## See Also

- `ToolCallStreamBuilder` - Helper for building tool call event sequences
- `ToolCallValidator` - Validation utilities for tool calls
- `AGUITool` - Tool definition class
- `ToolMessage` - Tool result message class
- `AssistantMessage.ToolCalls` - Completed tool calls in message history
